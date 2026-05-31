using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.Comments;

/// <summary>Read model for a comment, returned across the Application boundary.</summary>
public sealed record CommentDto(Guid Id, Guid RequestId, Guid AuthorId, string Body, DateTimeOffset CreatedAtUtc)
{
    public static CommentDto FromDomain(Comment comment) =>
        new(comment.Id, comment.RequestId, comment.AuthorId, comment.Body, comment.CreatedAtUtc);
}
