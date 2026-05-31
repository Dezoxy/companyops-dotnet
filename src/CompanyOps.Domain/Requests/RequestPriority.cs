namespace CompanyOps.Domain.Requests;

/// <summary>
/// How urgent a request is. Metadata only — priority does <em>not</em> change the approval
/// chain or the state machine; it informs triage and ordering. Applies to every request type.
/// </summary>
public enum RequestPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}
