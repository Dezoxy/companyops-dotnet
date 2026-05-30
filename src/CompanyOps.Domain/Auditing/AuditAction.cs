namespace CompanyOps.Domain.Auditing;

/// <summary>
/// The business action an <see cref="AuditLog"/> entry records. These are domain
/// operations (what happened), not CRUD verbs — one per meaningful state change.
/// </summary>
public enum AuditAction
{
    RequestCreated = 0,
    RequestSubmitted = 1,
    RequestApproved = 2,
    RequestRejected = 3,
    RequestFulfilled = 4,
}
