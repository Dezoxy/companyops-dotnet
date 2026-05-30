namespace CompanyOps.Api.Contracts;

/// <summary>
/// Body for <c>POST /requests/{id}/reject</c>. The approver identity comes from the
/// authenticated principal — the body carries only the (required) reason.
/// </summary>
public sealed record RejectRequestBody(string Reason);
