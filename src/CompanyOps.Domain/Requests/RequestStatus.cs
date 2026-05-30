namespace CompanyOps.Domain.Requests;

/// <summary>
/// Lifecycle states of a <see cref="Request"/>. Phase 1 only ever creates requests
/// in <see cref="Draft"/>; the transitions between these states (the state machine)
/// are added in Phase 2 and driven by the configured approval chain (ADR 0005).
/// </summary>
public enum RequestStatus
{
    Draft = 0,
    Submitted = 1,
    ManagerApproved = 2,
    FinanceApproved = 3,
    Rejected = 4,
    InFulfillment = 5,
    Completed = 6,
    Cancelled = 7,
}
