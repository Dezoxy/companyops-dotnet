using CompanyOps.Domain.Common;

namespace CompanyOps.Domain.Requests;

/// <summary>
/// A request raised by an employee that flows through an approval → fulfillment
/// lifecycle. This is the aggregate root of the workflow engine and the single place
/// the workflow rules are enforced.
/// <para>
/// The approval chain is data-driven (ADR 0005/0006): <see cref="Submit"/> materializes
/// the steps for this request's <see cref="RequestType"/> from <see cref="ApprovalChains"/>,
/// and the transitions are computed from those steps — "approved" means every required
/// step is satisfied, and the next decision falls to the first pending step. Illegal
/// transitions throw <see cref="DomainException"/>; they are never silently ignored.
/// </para>
/// </summary>
public class Request
{
    public const int TitleMaxLength = 200;

    private readonly List<ApprovalStep> _approvalSteps = [];

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public RequestType Type { get; private set; }
    public RequestStatus Status { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>The materialized approval chain, in order. Empty until the request is submitted.</summary>
    public IReadOnlyList<ApprovalStep> ApprovalSteps => _approvalSteps.AsReadOnly();

    // Required by EF Core's materializer; not for application use.
    private Request()
    {
    }

    private Request(
        Guid id,
        string title,
        string? description,
        RequestType type,
        Guid requesterId,
        Guid departmentId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
        Status = RequestStatus.Draft;
        RequesterId = requesterId;
        DepartmentId = departmentId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Factory for a new request. Enforces the creation invariants in the Domain
    /// (throws <see cref="DomainException"/>) rather than trusting the caller.
    /// </summary>
    public static Request Create(
        string title,
        string? description,
        RequestType type,
        Guid requesterId,
        Guid departmentId,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Request title is required.");
        }

        title = title.Trim();
        if (title.Length > TitleMaxLength)
        {
            throw new DomainException($"Request title must be at most {TitleMaxLength} characters.");
        }

        if (requesterId == Guid.Empty)
        {
            throw new DomainException("Request must have a requester.");
        }

        if (departmentId == Guid.Empty)
        {
            throw new DomainException("Request must belong to a department.");
        }

        return new Request(Guid.NewGuid(), title, description?.Trim(), type, requesterId, departmentId, nowUtc);
    }

    /// <summary>
    /// Submit the request for approval: <c>Draft → Submitted</c>. Only the requester may
    /// submit their own request. Materializes the approval chain configured for this
    /// request's type. The chain is fixed at submit time, so later config changes don't
    /// mutate in-flight requests.
    /// </summary>
    public void Submit(Guid actorId, DateTimeOffset nowUtc)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException("Submitting must record who performed it.");
        }

        if (actorId != RequesterId)
        {
            throw new DomainException("Only the requester can submit their own request.");
        }

        if (Status != RequestStatus.Draft)
        {
            throw new DomainException($"Only a draft request can be submitted; this request is {Status}.");
        }

        foreach (var definition in ApprovalChains.For(Type).OrderBy(step => step.Order))
        {
            _approvalSteps.Add(ApprovalStep.FromDefinition(definition));
        }

        // Defensive: a configured-but-empty chain would leave the request submitted with
        // no way to progress (and "all required steps approved" is vacuously true). A
        // request must have at least one approver.
        if (_approvalSteps.Count == 0)
        {
            throw new DomainException($"The approval chain for '{Type}' has no steps; a request cannot be submitted without an approver.");
        }

        _ = nowUtc; // reserved for SubmittedAtUtc when audit lands (Phase 4)
        Status = RequestStatus.Submitted;
    }

    /// <summary>
    /// Approve the current (first pending) step. The approver must hold the step's
    /// required role, and for a department-scoped step must belong to this request's
    /// department. When all required steps are approved, the request becomes
    /// <see cref="RequestStatus.Approved"/>.
    /// </summary>
    /// <remarks>
    /// The approver identity is passed in because there is no authenticated principal
    /// until Phase 3 (the same temporary bridge as <see cref="RequesterId"/>). From
    /// Phase 3 the source becomes the JWT principal; this rule does not change.
    /// </remarks>
    public void Approve(Guid approverId, ApproverRole approverRole, Guid approverDepartmentId, DateTimeOffset nowUtc, string? note = null)
    {
        var step = EnsureDecidableBy(approverId, approverRole, approverDepartmentId);
        step.Approve(approverId, nowUtc, note);

        if (_approvalSteps.Where(s => s.IsRequired).All(s => s.Decision == ApprovalDecision.Approved))
        {
            Status = RequestStatus.Approved;
        }
    }

    /// <summary>
    /// Reject the current (first pending) step. Eligibility is the same as approval.
    /// A rejection is terminal: the request becomes <see cref="RequestStatus.Rejected"/>.
    /// </summary>
    public void Reject(Guid approverId, ApproverRole approverRole, Guid approverDepartmentId, DateTimeOffset nowUtc, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A rejection reason is required.");
        }

        var step = EnsureDecidableBy(approverId, approverRole, approverDepartmentId);
        step.Reject(approverId, nowUtc, reason.Trim());
        Status = RequestStatus.Rejected;
    }

    /// <summary>
    /// Fulfill an approved request: <c>Approved → Completed</c>. Synchronous for now;
    /// the <see cref="RequestStatus.InFulfillment"/> state becomes meaningful when the
    /// Phase 5 worker performs fulfillment asynchronously.
    /// </summary>
    public void Fulfill(Guid actorId, DateTimeOffset nowUtc)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException("Fulfillment must record who performed it.");
        }

        if (Status != RequestStatus.Approved)
        {
            throw new DomainException($"Only an approved request can be fulfilled; this request is {Status}.");
        }

        _ = nowUtc; // reserved for the fulfillment timestamp when audit lands (Phase 4)
        Status = RequestStatus.Completed;
    }

    /// <summary>
    /// Validates that <paramref name="approverId"/> with <paramref name="approverRole"/>
    /// may decide the current step, and returns that step. Throws otherwise.
    /// </summary>
    private ApprovalStep EnsureDecidableBy(Guid approverId, ApproverRole approverRole, Guid approverDepartmentId)
    {
        if (Status != RequestStatus.Submitted)
        {
            throw new DomainException($"Only a submitted request can be decided; this request is {Status}.");
        }

        if (approverId == Guid.Empty)
        {
            throw new DomainException("An approval decision must record who made it.");
        }

        var current = _approvalSteps.FirstOrDefault(s => s.Decision == ApprovalDecision.Pending)
            ?? throw new DomainException("There is no pending approval step to decide.");

        if (approverRole != current.RequiredRole)
        {
            throw new DomainException($"Approval step {current.Order} requires the {current.RequiredRole} role.");
        }

        if (current.Scope == ApprovalScope.Department && approverDepartmentId != DepartmentId)
        {
            throw new DomainException($"Approval step {current.Order} is department-scoped and must be decided by the request's department.");
        }

        return current;
    }
}
