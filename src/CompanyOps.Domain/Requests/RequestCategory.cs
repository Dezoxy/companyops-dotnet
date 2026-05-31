namespace CompanyOps.Domain.Requests;

/// <summary>
/// Classification for a <see cref="RequestType.Helpdesk"/> request (incident vs. service vs.
/// access). Helpdesk-only: it is required to be <c>null</c> for other request types — the
/// <see cref="Request.Create"/> factory enforces that invariant. Absence is a <c>null</c>
/// <c>RequestCategory?</c> — do not add a <c>None</c> member.
/// </summary>
public enum RequestCategory
{
    Incident = 0,
    ServiceRequest = 1,
    AccessRequest = 2,
}
