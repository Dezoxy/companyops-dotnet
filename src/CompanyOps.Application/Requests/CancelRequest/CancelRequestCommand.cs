using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.CancelRequest;

/// <summary>
/// Use-case input for cancelling a request. <paramref name="ActorId"/>, <paramref name="ActorRoles"/>,
/// and <paramref name="ActorDepartmentId"/> all come from the authenticated principal (never the
/// body); the Domain enforces that only the requester or a manager of the request's department may
/// cancel, and only from Draft or Submitted (before approval completes).
/// </summary>
public sealed record CancelRequestCommand(
    Guid RequestId,
    Guid ActorId,
    IReadOnlyCollection<ApproverRole> ActorRoles,
    Guid ActorDepartmentId);
