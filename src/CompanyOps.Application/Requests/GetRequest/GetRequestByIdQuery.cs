namespace CompanyOps.Application.Requests.GetRequest;

/// <summary>
/// Read a single request by id, within the caller's scope. At most one filter is set (the Api
/// derives it from the role): <see cref="RequesterId"/> → only the caller's own;
/// <see cref="DepartmentId"/> → only that department; both null → any. An out-of-scope id reads as
/// not-found (the handler returns null → 404), so existence isn't revealed.
/// </summary>
public sealed record GetRequestByIdQuery(Guid Id, Guid? RequesterId = null, Guid? DepartmentId = null);
