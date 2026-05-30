namespace CompanyOps.Api.Contracts;

/// <summary>
/// Body for <c>POST /requests/{id}/approve</c>. The approver identity (id, role,
/// department) comes from the authenticated principal — the body carries only the
/// optional note.
/// </summary>
public sealed record ApproveRequestBody(string? Note);
