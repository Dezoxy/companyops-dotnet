using CompanyOps.Domain.Common;

namespace CompanyOps.Domain.Requests;

/// <summary>
/// One step of a request's approval chain — the materialized instance of an
/// <see cref="ApprovalStepDefinition"/>, carrying the decision once made. Owned by
/// the <see cref="Request"/> aggregate. The mutators are <c>internal</c>: the Domain
/// assembly is the trust boundary, and by convention only <see cref="Request"/> calls
/// them, so the aggregate stays the single place that enforces ordering, role, and
/// scope. (C# has no "package-private to one class"; a friend/nested-class shim would
/// be premature here — and the per-step <see cref="ApprovalDecision.Pending"/> guard
/// below still prevents a step being decided twice regardless of caller.)
/// </summary>
public class ApprovalStep
{
    public Guid Id { get; private set; }
    public int Order { get; private set; }
    public ApproverRole RequiredRole { get; private set; }
    public ApprovalScope Scope { get; private set; }
    public bool IsRequired { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public Guid? DecidedById { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? Note { get; private set; }

    // Required by EF Core's materializer; not for application use.
    private ApprovalStep()
    {
    }

    private ApprovalStep(int order, ApproverRole requiredRole, ApprovalScope scope, bool isRequired)
    {
        Id = Guid.NewGuid();
        Order = order;
        RequiredRole = requiredRole;
        Scope = scope;
        IsRequired = isRequired;
        Decision = ApprovalDecision.Pending;
    }

    internal static ApprovalStep FromDefinition(ApprovalStepDefinition definition) =>
        new(definition.Order, definition.RequiredRole, definition.Scope, definition.IsRequired);

    internal void Approve(Guid approverId, DateTimeOffset nowUtc, string? note)
    {
        Decide(ApprovalDecision.Approved, approverId, nowUtc, note);
    }

    internal void Reject(Guid approverId, DateTimeOffset nowUtc, string reason)
    {
        Decide(ApprovalDecision.Rejected, approverId, nowUtc, reason);
    }

    private void Decide(ApprovalDecision decision, Guid approverId, DateTimeOffset nowUtc, string? note)
    {
        if (Decision != ApprovalDecision.Pending)
        {
            throw new DomainException($"Approval step {Order} has already been decided.");
        }

        Decision = decision;
        DecidedById = approverId;
        DecidedAtUtc = nowUtc;
        Note = note;
    }
}
