using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.Comments.AddComment;

/// <summary>
/// Adds a comment to a request's thread. Verifies the request exists <b>and is in the caller's
/// read scope</b> first (returns <c>null</c> → 404 otherwise) — you can only comment on a request
/// you're entitled to see. Comments are append-only and self-auditing (they carry author +
/// timestamp), so no separate <c>AuditLog</c> entry is written — a comment is discussion, not a
/// workflow state change.
/// </summary>
public sealed class AddCommentHandler(
    IRequestRepository requests,
    ICommentRepository comments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CommentDto?> HandleAsync(AddCommentCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(command.RequestId, cancellationToken);
        if (request is null || RequestReadScope.IsOutOfScope(request, command.RequesterId, command.DepartmentId))
        {
            return null;
        }

        var comment = Comment.Create(command.RequestId, command.AuthorId, command.Body, timeProvider.GetUtcNow());
        comments.Add(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CommentDto.FromDomain(comment);
    }
}
