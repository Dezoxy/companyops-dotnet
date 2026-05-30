using CompanyOps.Domain.Requests;

namespace CompanyOps.Api.Contracts;

/// <summary>
/// Body for <c>POST /requests/{id}/reject</c>. The approver identity is in the body
/// only because there is no authentication yet; from Phase 3 it comes from the
/// authenticated principal and is removed from here.
/// </summary>
public sealed record RejectRequestBody(
    Guid ApproverId,
    ApproverRole ApproverRole,
    Guid ApproverDepartmentId,
    string Reason);
