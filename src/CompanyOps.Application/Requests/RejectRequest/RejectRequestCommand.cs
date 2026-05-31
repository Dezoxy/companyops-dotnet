using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.RejectRequest;

/// <summary>
/// Use-case input for rejecting the current step of a submitted request. Eligibility
/// matches approval (role + department scope); a rejection is terminal.
/// </summary>
/// <remarks>
/// The approver identity comes from the authenticated principal, not the body.
/// <paramref name="ApproverRoles"/> is the actor's full set of approver-capable roles; the Domain
/// matches the current step's required role against the set (same eligibility as approval).
/// </remarks>
public sealed record RejectRequestCommand(
    Guid RequestId,
    Guid ApproverId,
    IReadOnlyCollection<ApproverRole> ApproverRoles,
    Guid ApproverDepartmentId,
    string Reason);
