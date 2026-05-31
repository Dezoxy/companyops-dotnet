using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Domain.Tests.Requests;

/// <summary>
/// Covers the Phase 2 approval state machine on <see cref="Request"/>: submit,
/// step-driven approve/reject (with department scope), and fulfill. Transitions are
/// computed from the configured chain (ADR 0005/0006); illegal moves throw.
/// The procurement chain is: Manager (department-scoped) → Finance (global).
/// </summary>
public class ApprovalWorkflowTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Department = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherDepartment = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ManagerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FinanceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static Request NewDraft(RequestType type = RequestType.Procurement) =>
        Request.Create("New laptop", "spec", type, Requester, Department, Now);

    private static Request NewSubmitted()
    {
        var request = NewDraft();
        request.Submit(Requester, Now);
        return request;
    }

    // --- Submit ---------------------------------------------------------------

    [Fact]
    public void Submit_FromDraft_MaterializesChainInOrderAndSetsSubmitted()
    {
        var request = NewDraft();

        request.Submit(Requester, Now);

        Assert.Equal(RequestStatus.Submitted, request.Status);
        Assert.Collection(
            request.ApprovalSteps,
            first =>
            {
                Assert.Equal(1, first.Order);
                Assert.Equal(ApproverRole.Manager, first.RequiredRole);
                Assert.Equal(ApprovalScope.Department, first.Scope);
                Assert.Equal(ApprovalDecision.Pending, first.Decision);
            },
            second =>
            {
                Assert.Equal(2, second.Order);
                Assert.Equal(ApproverRole.Finance, second.RequiredRole);
                Assert.Equal(ApprovalScope.Global, second.Scope);
                Assert.Equal(ApprovalDecision.Pending, second.Decision);
            });
    }

    [Fact]
    public void Submit_WhenAlreadySubmitted_ThrowsAndDoesNotReMaterializeSteps()
    {
        var request = NewSubmitted();

        Assert.Throws<DomainException>(() => request.Submit(Requester, Now));
        Assert.Equal(2, request.ApprovalSteps.Count); // guard runs before any steps are re-added
    }

    [Fact]
    public void Submit_ForRequestTypeWithNoConfiguredChain_ThrowsDomainException()
    {
        // AssetLifecycle has no chain yet (lands in Phase 16); Helpdesk now does (Phase 15).
        var request = NewDraft(RequestType.AssetLifecycle);

        var ex = Assert.Throws<DomainException>(() => request.Submit(Requester, Now));
        Assert.Contains("No approval chain", ex.Message);
    }

    [Fact]
    public void Submit_ByNonRequester_ThrowsDomainException()
    {
        var request = NewDraft();
        var someoneElse = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var ex = Assert.Throws<DomainException>(() => request.Submit(someoneElse, Now));
        Assert.Contains("requester", ex.Message);
        Assert.Equal(RequestStatus.Draft, request.Status);
    }

    [Fact]
    public void Submit_WithEmptyActorId_ThrowsDomainException()
    {
        var request = NewDraft();

        Assert.Throws<DomainException>(() => request.Submit(Guid.Empty, Now));
    }

    // --- Helpdesk chain (manager-only, Phase 15) ------------------------------

    [Fact]
    public void Submit_Helpdesk_MaterializesSingleManagerStep()
    {
        var request = NewDraft(RequestType.Helpdesk);

        request.Submit(Requester, Now);

        var step = Assert.Single(request.ApprovalSteps);
        Assert.Equal(ApproverRole.Manager, step.RequiredRole);
        Assert.Equal(ApprovalScope.Department, step.Scope);
    }

    [Fact]
    public void Approve_Helpdesk_ByManager_ReachesApprovedInOneStep()
    {
        var request = NewDraft(RequestType.Helpdesk);
        request.Submit(Requester, Now);

        request.Approve(ManagerId, ApproverRole.Manager, Department, Now);

        Assert.Equal(RequestStatus.Approved, request.Status);
    }

    // --- Approve --------------------------------------------------------------

    [Fact]
    public void Approve_ByManagerInDepartment_ApprovesFirstStepButStaysSubmitted()
    {
        var request = NewSubmitted();

        request.Approve(ManagerId, ApproverRole.Manager, Department, Now);

        Assert.Equal(RequestStatus.Submitted, request.Status); // finance still pending
        Assert.Equal(ApprovalDecision.Approved, request.ApprovalSteps[0].Decision);
        Assert.Equal(ManagerId, request.ApprovalSteps[0].DecidedById);
        Assert.Equal(ApprovalDecision.Pending, request.ApprovalSteps[1].Decision);
    }

    [Fact]
    public void Approve_AllRequiredSteps_SetsApproved()
    {
        var request = NewSubmitted();

        request.Approve(ManagerId, ApproverRole.Manager, Department, Now);
        request.Approve(FinanceId, ApproverRole.Finance, OtherDepartment, Now); // global step: dept ignored

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.All(request.ApprovalSteps, step => Assert.Equal(ApprovalDecision.Approved, step.Decision));
    }

    [Fact]
    public void Approve_ByWrongRoleForCurrentStep_ThrowsDomainException()
    {
        var request = NewSubmitted(); // current step requires Manager

        Assert.Throws<DomainException>(
            () => request.Approve(FinanceId, ApproverRole.Finance, Department, Now));
    }

    [Fact]
    public void Approve_ByManagerFromAnotherDepartment_ThrowsDomainException()
    {
        var request = NewSubmitted();

        var ex = Assert.Throws<DomainException>(
            () => request.Approve(ManagerId, ApproverRole.Manager, OtherDepartment, Now));
        Assert.Contains("department-scoped", ex.Message);
    }

    [Fact]
    public void Approve_WhenStillDraft_ThrowsDomainException()
    {
        var request = NewDraft();

        Assert.Throws<DomainException>(
            () => request.Approve(ManagerId, ApproverRole.Manager, Department, Now));
    }

    [Fact]
    public void Approve_WithEmptyApproverId_ThrowsDomainException()
    {
        var request = NewSubmitted();

        Assert.Throws<DomainException>(
            () => request.Approve(Guid.Empty, ApproverRole.Manager, Department, Now));
    }

    // --- Reject ---------------------------------------------------------------

    [Fact]
    public void Reject_ByManagerAtFirstStep_SetsRejected()
    {
        var request = NewSubmitted();

        request.Reject(ManagerId, ApproverRole.Manager, Department, Now, "Out of budget");

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal(ApprovalDecision.Rejected, request.ApprovalSteps[0].Decision);
        Assert.Equal("Out of budget", request.ApprovalSteps[0].Note);
    }

    [Fact]
    public void Reject_AtFinanceStepAfterManagerApproval_SetsRejected()
    {
        var request = NewSubmitted();
        request.Approve(ManagerId, ApproverRole.Manager, Department, Now);

        request.Reject(FinanceId, ApproverRole.Finance, OtherDepartment, Now, "No funds this quarter");

        Assert.Equal(RequestStatus.Rejected, request.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithoutReason_ThrowsDomainException(string reason)
    {
        var request = NewSubmitted();

        Assert.Throws<DomainException>(
            () => request.Reject(ManagerId, ApproverRole.Manager, Department, Now, reason));
    }

    [Fact]
    public void Approve_AfterRejected_ThrowsDomainException()
    {
        var request = NewSubmitted();
        request.Reject(ManagerId, ApproverRole.Manager, Department, Now, "No");

        Assert.Throws<DomainException>(
            () => request.Approve(ManagerId, ApproverRole.Manager, Department, Now));
    }

    [Fact]
    public void Approve_WhenAlreadyFullyApproved_ThrowsWithStatusBasedMessage()
    {
        var request = NewSubmitted();
        request.Approve(ManagerId, ApproverRole.Manager, Department, Now);
        request.Approve(FinanceId, ApproverRole.Finance, OtherDepartment, Now); // now Approved

        // The status guard fires before the pending-step lookup, so the message names
        // the request state rather than a confusing "no pending step".
        var ex = Assert.Throws<DomainException>(
            () => request.Approve(FinanceId, ApproverRole.Finance, OtherDepartment, Now));
        Assert.Contains("Approved", ex.Message);
    }

    // --- Fulfill --------------------------------------------------------------

    [Fact]
    public void Fulfill_WhenApproved_SetsCompleted()
    {
        var request = NewSubmitted();
        request.Approve(ManagerId, ApproverRole.Manager, Department, Now);
        request.Approve(FinanceId, ApproverRole.Finance, OtherDepartment, Now);

        request.Fulfill(FinanceId, Now);

        Assert.Equal(RequestStatus.Completed, request.Status);
    }

    [Fact]
    public void Fulfill_WhenNotYetApproved_ThrowsDomainException()
    {
        var request = NewSubmitted();
        request.Approve(ManagerId, ApproverRole.Manager, Department, Now); // still Submitted

        Assert.Throws<DomainException>(() => request.Fulfill(FinanceId, Now));
    }

    [Fact]
    public void Fulfill_WithEmptyActorId_ThrowsDomainException()
    {
        var request = NewSubmitted();
        request.Approve(ManagerId, ApproverRole.Manager, Department, Now);
        request.Approve(FinanceId, ApproverRole.Finance, OtherDepartment, Now);

        Assert.Throws<DomainException>(() => request.Fulfill(Guid.Empty, Now));
    }
}
