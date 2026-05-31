using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests;

/// <summary>
/// The read-scope rule shared by the single-request read and the comment thread. The Api derives
/// the filter from the caller's role and sets at most one: <c>requesterId</c> → only the caller's
/// own, <c>departmentId</c> → only that department, both null → any (the oversight roles). A
/// request is out of scope when a set filter doesn't match its requester / department; handlers
/// then return null so the Api maps it to 404 — a request's existence isn't revealed to someone
/// not entitled to see it.
/// </summary>
internal static class RequestReadScope
{
    internal static bool IsOutOfScope(Request request, Guid? requesterId, Guid? departmentId) =>
        (requesterId is { } r && request.RequesterId != r) ||
        (departmentId is { } d && request.DepartmentId != d);
}
