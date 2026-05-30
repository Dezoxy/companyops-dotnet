namespace CompanyOps.Domain.Requests;

/// <summary>
/// The outcome of a single <see cref="ApprovalStep"/>. A step starts
/// <see cref="Pending"/>; the overall request is approved when every required step
/// is <see cref="Approved"/>, and rejected as soon as one step is <see cref="Rejected"/>.
/// </summary>
public enum ApprovalDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
