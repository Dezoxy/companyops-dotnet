using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.ApproveRequest;

/// <summary>
/// Use-case input for approving the current step of a submitted request. There is one
/// step-driven approve action (ADR 0006): the approver's role and the configured chain
/// determine which step is decided.
/// </summary>
/// <remarks>
/// The approver identity (<paramref name="ApproverId"/>, <paramref name="ApproverRole"/>,
/// <paramref name="ApproverDepartmentId"/>) is supplied by the caller as the pre-auth
/// bridge — from Phase 3 it comes from the authenticated principal, not the body.
/// </remarks>
public sealed record ApproveRequestCommand(
    Guid RequestId,
    Guid ApproverId,
    ApproverRole ApproverRole,
    Guid ApproverDepartmentId,
    string? Note);
