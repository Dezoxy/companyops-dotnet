namespace CompanyOps.Api.Contracts;

/// <summary>
/// Body for <c>POST /requests/{id}/comments</c>. The author comes from the authenticated
/// principal (the JWT sub) — never the body.
/// </summary>
public sealed record AddCommentRequest(string Body);
