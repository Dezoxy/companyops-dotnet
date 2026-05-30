namespace CompanyOps.Domain.Requests;

/// <summary>
/// Overall lifecycle state of a <see cref="Request"/>. Chain-agnostic by design
/// (ADR 0006): it describes the request as a whole, not any one approval chain.
/// <em>Which</em> approvers signed off lives per-step in <see cref="ApprovalStep"/>,
/// not here — so the same status set serves procurement, helpdesk, and asset flows.
/// <para>
/// Path: <c>Draft → Submitted → Approved → InFulfillment → Completed</c>, with
/// <see cref="Rejected"/> and <see cref="Cancelled"/> as terminal branches.
/// "Approved" means every required step is satisfied. Invalid transitions throw in
/// the Domain (see <see cref="Request"/>).
/// </para>
/// </summary>
public enum RequestStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,

    // Not reachable through a public transition yet — Fulfill goes Approved → Completed
    // synchronously in Phase 2. This state becomes meaningful when the Phase 5 worker
    // performs fulfillment asynchronously (ADR 0005).
    InFulfillment = 3,
    Completed = 4,
    Rejected = 5,
    Cancelled = 6,
}
