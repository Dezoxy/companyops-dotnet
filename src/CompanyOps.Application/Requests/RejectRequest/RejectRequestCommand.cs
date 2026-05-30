using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.RejectRequest;

/// <summary>
/// Use-case input for rejecting the current step of a submitted request. Eligibility
/// matches approval (role + department scope); a rejection is terminal.
/// </summary>
/// <remarks>
/// The approver identity is the pre-auth bridge — from Phase 3 it comes from the
/// authenticated principal, not the body.
/// </remarks>
public sealed record RejectRequestCommand(
    Guid RequestId,
    Guid ApproverId,
    ApproverRole ApproverRole,
    Guid ApproverDepartmentId,
    string Reason);
