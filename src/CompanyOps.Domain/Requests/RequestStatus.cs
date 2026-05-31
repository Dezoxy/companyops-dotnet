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

    // Reserved — not currently reached. Fulfillment is synchronous (Approved → Completed); the
    // async worker handles external integration (ADR 0008), not this status. It becomes
    // meaningful only if fulfillment itself is made asynchronous.
    InFulfillment = 3,
    Completed = 4,
    Rejected = 5,

    // Reserved — no Cancel transition is built yet (tracked in docs/security.md). Status has a
    // private setter, so only a (future) Domain Cancel method could ever set this.
    Cancelled = 6,
}
