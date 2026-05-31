using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Application.Requests.ApproveRequest;
using CompanyOps.Application.Requests.CancelRequest;
using CompanyOps.Application.Requests.CreateRequest;
using CompanyOps.Application.Requests.FulfillRequest;
using CompanyOps.Application.Requests.RejectRequest;
using CompanyOps.Application.Requests.SubmitRequest;
using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Auditing;
using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;
using Xunit;

namespace CompanyOps.Application.Tests;

/// <summary>
/// Handler orchestration: each command handler loads, calls the domain, audits, enqueues
/// the right event (and only then), and persists. Asserted with fakes — no database.
/// </summary>
public class RequestHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Department = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ManagerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FinanceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ItAdminId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly FakeRequestRepository _requests = new();
    private readonly FakeAssetRepository _assets = new();
    private readonly CapturingAuditLogger _audit = new();
    private readonly CapturingEventPublisher _events = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FixedTimeProvider _clock = new(Now);

    // --- Create ---------------------------------------------------------------

    [Fact]
    public async Task Create_StoresDraftAndAuditsCreated_WithoutEvent()
    {
        var handler = new CreateRequestHandler(_requests, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new CreateRequestCommand("Laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department));

        Assert.Equal("Draft", dto.Status.ToString());
        Assert.True(_requests.Store.ContainsKey(dto.Id));
        Assert.Equal(1, _uow.SaveCount);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.RequestCreated && e.TargetId == dto.Id);
        Assert.Empty(_events.Events);
    }

    // --- Submit ---------------------------------------------------------------

    [Fact]
    public async Task Submit_ByRequester_AuditsSubmitted_NoEvent()
    {
        var request = Request.Create("Laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, Now);
        _requests.Seed(request);
        var handler = new SubmitRequestHandler(_requests, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new SubmitRequestCommand(request.Id, Requester));

        Assert.Equal("Submitted", dto!.Status.ToString());
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.RequestSubmitted);
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Submit_MissingRequest_ReturnsNull()
    {
        var handler = new SubmitRequestHandler(_requests, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new SubmitRequestCommand(Guid.NewGuid(), Requester));

        Assert.Null(dto);
        Assert.Empty(_audit.Entries);
        Assert.Equal(0, _uow.SaveCount);
    }

    // --- Approve --------------------------------------------------------------

    [Fact]
    public async Task Approve_IntermediateStep_AuditsButDoesNotEnqueue()
    {
        var request = Submitted();
        _requests.Seed(request);
        var handler = new ApproveRequestHandler(_requests, _audit, _events, _uow, _clock);

        // Manager step: request stays Submitted (finance still pending).
        var dto = await handler.HandleAsync(new ApproveRequestCommand(request.Id, ManagerId, [ApproverRole.Manager], Department, "ok"));

        Assert.Equal("Submitted", dto!.Status.ToString());
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.RequestApproved);
        Assert.Empty(_events.Events); // not fully approved yet
    }

    [Fact]
    public async Task Approve_FinalStep_EnqueuesRequestApproved()
    {
        var request = Submitted();
        request.Approve(ManagerId, [ApproverRole.Manager], Department, Now); // manager step done
        _requests.Seed(request);
        var handler = new ApproveRequestHandler(_requests, _audit, _events, _uow, _clock);

        var dto = await handler.HandleAsync(new ApproveRequestCommand(request.Id, FinanceId, [ApproverRole.Finance], Department, null));

        Assert.Equal("Approved", dto!.Status.ToString());
        var approved = Assert.Single(_events.Events);
        var evt = Assert.IsType<RequestApproved>(approved);
        Assert.Equal(request.Id, evt.RequestId);
    }

    [Fact]
    public async Task Approve_MissingRequest_ReturnsNull()
    {
        var handler = new ApproveRequestHandler(_requests, _audit, _events, _uow, _clock);

        var dto = await handler.HandleAsync(new ApproveRequestCommand(Guid.NewGuid(), ManagerId, [ApproverRole.Manager], Department, null));

        Assert.Null(dto);
        Assert.Empty(_events.Events);
    }

    // --- Reject ---------------------------------------------------------------

    [Fact]
    public async Task Reject_AuditsRejected_NoEvent()
    {
        var request = Submitted();
        _requests.Seed(request);
        var handler = new RejectRequestHandler(_requests, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new RejectRequestCommand(request.Id, ManagerId, [ApproverRole.Manager], Department, "Over budget"));

        Assert.Equal("Rejected", dto!.Status.ToString());
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.RequestRejected);
        Assert.Empty(_events.Events);
    }

    // --- Fulfill --------------------------------------------------------------

    [Fact]
    public async Task Fulfill_Procurement_EnqueuesRequestFulfilled_AndAudits()
    {
        var request = Submitted();
        request.Approve(ManagerId, [ApproverRole.Manager], Department, Now);
        request.Approve(FinanceId, [ApproverRole.Finance], Department, Now); // now Approved
        _requests.Seed(request);
        var handler = new FulfillRequestHandler(_requests, _assets, _audit, _events, _uow, _clock);

        var dto = await handler.HandleAsync(new FulfillRequestCommand(request.Id, ItAdminId));

        Assert.Equal("Completed", dto!.Status.ToString());
        Assert.Null(dto.FulfilledAssetId);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.RequestFulfilled);
        var fulfilled = Assert.Single(_events.Events); // external-inventory reservation path
        Assert.IsType<RequestFulfilled>(fulfilled);
    }

    [Fact]
    public async Task Fulfill_AssetLifecycle_AssignsAssetToRequester_LinksIt_NoEvent()
    {
        var request = ApprovedAssetRequest();
        _requests.Seed(request);
        var asset = Asset.Register("AST-1", "MacBook Pro", AssetType.Laptop, Now);
        _assets.Seed(asset);
        var handler = new FulfillRequestHandler(_requests, _assets, _audit, _events, _uow, _clock);

        var dto = await handler.HandleAsync(new FulfillRequestCommand(request.Id, ItAdminId, asset.Id));

        Assert.Equal("Completed", dto!.Status.ToString());
        Assert.Equal(asset.Id, dto.FulfilledAssetId);             // request → asset link recorded
        Assert.Equal(AssetStatus.Assigned, asset.Status);          // the real internal transition happened
        Assert.Equal(Requester, asset.AssignedToId);               // assigned to the requester, not the actor
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.RequestFulfilled);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.AssetAssigned && e.TargetId == asset.Id);
        Assert.Empty(_events.Events);                              // internal assign, not the external-inventory path
    }

    [Fact]
    public async Task Fulfill_AssetLifecycle_WithoutAsset_Throws()
    {
        var request = ApprovedAssetRequest();
        _requests.Seed(request);
        var handler = new FulfillRequestHandler(_requests, _assets, _audit, _events, _uow, _clock);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.HandleAsync(new FulfillRequestCommand(request.Id, ItAdminId))); // no asset id
    }

    [Fact]
    public async Task Fulfill_AssetLifecycle_AssetNotInStock_Throws()
    {
        var request = ApprovedAssetRequest();
        _requests.Seed(request);
        var asset = Asset.Register("AST-2", "ThinkPad", AssetType.Laptop, Now);
        asset.Assign(Guid.NewGuid(), Now); // already assigned → not in stock
        _assets.Seed(asset);
        var handler = new FulfillRequestHandler(_requests, _assets, _audit, _events, _uow, _clock);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.HandleAsync(new FulfillRequestCommand(request.Id, ItAdminId, asset.Id)));
    }

    // --- Cancel ---------------------------------------------------------------

    [Fact]
    public async Task Cancel_DraftByRequester_AuditsCancelled()
    {
        var request = Request.Create("Laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, Now);
        _requests.Seed(request);
        var handler = new CancelRequestHandler(_requests, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new CancelRequestCommand(request.Id, Requester));

        Assert.Equal("Cancelled", dto!.Status.ToString());
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.RequestCancelled && e.TargetId == request.Id);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Cancel_MissingRequest_ReturnsNull()
    {
        var handler = new CancelRequestHandler(_requests, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new CancelRequestCommand(Guid.NewGuid(), Requester));

        Assert.Null(dto);
    }

    private static Request Submitted()
    {
        var request = Request.Create("Laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, Now);
        request.Submit(Requester, Now);
        return request;
    }

    // An asset-lifecycle request advanced to Approved: its chain is manager-only, so a single
    // department-scoped manager approval clears it.
    private static Request ApprovedAssetRequest()
    {
        var request = Request.Create("Need a laptop", null, RequestType.AssetLifecycle, RequestPriority.Medium, null, Requester, Department, Now);
        request.Submit(Requester, Now);
        request.Approve(ManagerId, [ApproverRole.Manager], Department, Now);
        return request;
    }
}
