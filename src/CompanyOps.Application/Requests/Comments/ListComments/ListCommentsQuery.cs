namespace CompanyOps.Application.Requests.Comments.ListComments;

/// <summary>
/// Read a request's comment thread, within the caller's read scope. The Api sets at most one of
/// <see cref="RequesterId"/> / <see cref="DepartmentId"/> from the role (both null = oversight,
/// any). If the request is out of scope the handler returns null → 404, so the thread (and the
/// request's existence) isn't revealed to someone not entitled to see the request.
/// </summary>
public sealed record ListCommentsQuery(Guid RequestId, Guid? RequesterId = null, Guid? DepartmentId = null);
