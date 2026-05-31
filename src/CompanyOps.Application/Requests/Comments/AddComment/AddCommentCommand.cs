namespace CompanyOps.Application.Requests.Comments.AddComment;

/// <summary>
/// Use-case input for adding a comment. <paramref name="AuthorId"/> comes from the
/// authenticated principal (the JWT sub), never the request body.
/// </summary>
public sealed record AddCommentCommand(Guid RequestId, Guid AuthorId, string Body);
