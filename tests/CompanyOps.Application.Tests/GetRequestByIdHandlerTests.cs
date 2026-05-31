using CompanyOps.Application.Requests.GetRequest;
using CompanyOps.Domain.Requests;
using Xunit;

namespace CompanyOps.Application.Tests;

/// <summary>
/// Read-scoping on the single-request read. The Api passes at most one filter (derived from the
/// caller's role); the handler returns the request only when it's in scope, otherwise null so the
/// Api maps it to 404 — an out-of-scope id must not reveal the request's existence. Fakes, no DB.
/// </summary>
public class GetRequestByIdHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Department = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Other = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly FakeRequestRepository _requests = new();

    private Request Seed()
    {
        var request = Request.Create("Laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, Now);
        _requests.Seed(request);
        return request;
    }

    [Fact]
    public async Task HandleAsync_WhenRequestDoesNotExist_ReturnsNull()
    {
        var handler = new GetRequestByIdHandler(_requests);

        var dto = await handler.HandleAsync(new GetRequestByIdQuery(Guid.NewGuid()));

        Assert.Null(dto);
    }

    [Fact]
    public async Task HandleAsync_WithNoScopeFilter_ReturnsRequest()
    {
        var request = Seed();
        var handler = new GetRequestByIdHandler(_requests);

        var dto = await handler.HandleAsync(new GetRequestByIdQuery(request.Id)); // Finance / IT Admin / Auditor

        Assert.NotNull(dto);
        Assert.Equal(request.Id, dto!.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenRequesterMatches_ReturnsRequest()
    {
        var request = Seed();
        var handler = new GetRequestByIdHandler(_requests);

        var dto = await handler.HandleAsync(new GetRequestByIdQuery(request.Id, RequesterId: Requester));

        Assert.NotNull(dto);
    }

    [Fact]
    public async Task HandleAsync_WhenRequesterDiffers_ReturnsNull()
    {
        var request = Seed();
        var handler = new GetRequestByIdHandler(_requests);

        var dto = await handler.HandleAsync(new GetRequestByIdQuery(request.Id, RequesterId: Other));

        Assert.Null(dto); // someone else's request — reads as absent
    }

    [Fact]
    public async Task HandleAsync_WhenDepartmentMatches_ReturnsRequest()
    {
        var request = Seed();
        var handler = new GetRequestByIdHandler(_requests);

        var dto = await handler.HandleAsync(new GetRequestByIdQuery(request.Id, DepartmentId: Department));

        Assert.NotNull(dto);
    }

    [Fact]
    public async Task HandleAsync_WhenDepartmentDiffers_ReturnsNull()
    {
        var request = Seed();
        var handler = new GetRequestByIdHandler(_requests);

        var dto = await handler.HandleAsync(new GetRequestByIdQuery(request.Id, DepartmentId: Other));

        Assert.Null(dto); // another department — reads as absent
    }
}
