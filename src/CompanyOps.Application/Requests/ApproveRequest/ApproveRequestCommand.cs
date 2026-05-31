using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.ApproveRequest;

/// <summary>
/// Use-case input for approving the current step of a submitted request. There is one
/// step-driven approve action (ADR 0006): the approver's role and the configured chain
/// determine which step is decided.
/// </summary>
/// <remarks>
/// The approver identity (<paramref name="ApproverId"/>, <paramref name="ApproverRoles"/>,
/// <paramref name="ApproverDepartmentId"/>) comes from the authenticated principal, not the body.
/// <paramref name="ApproverRoles"/> is the actor's full set of approver-capable roles; the Domain
/// matches the current step's required role against the set, so a user holding more than one
/// approver role isn't mis-assigned to the wrong step.
/// </remarks>
public sealed record ApproveRequestCommand(
    Guid RequestId,
    Guid ApproverId,
    IReadOnlyCollection<ApproverRole> ApproverRoles,
    Guid ApproverDepartmentId,
    string? Note);
