namespace CompanyOps.Application.Requests.Comments.AddComment;

/// <summary>
/// Use-case input for adding a comment. <paramref name="AuthorId"/> comes from the authenticated
/// principal (the JWT sub), never the request body. <paramref name="RequesterId"/> /
/// <paramref name="DepartmentId"/> are the caller's read scope (set by the Api from the role, at
/// most one): you can only comment on a request you're entitled to see — out of scope → null → 404.
/// </summary>
public sealed record AddCommentCommand(
    Guid RequestId,
    Guid AuthorId,
    string Body,
    Guid? RequesterId = null,
    Guid? DepartmentId = null);
