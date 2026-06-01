using CompanyOps.Application.Common;

namespace CompanyOps.Application.Requests.ListRequests;

/// <summary>
/// Read scope for the request list. At most one filter is set (the Api derives it from the
/// caller's role): <see cref="RequesterId"/> → the caller's own requests; <see cref="DepartmentId"/>
/// → that department; both null → all.
/// </summary>
public sealed record ListRequestsQuery(Guid? RequesterId = null, Guid? DepartmentId = null, PageRequest? Page = null);
