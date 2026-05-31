using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Requests.Comments.ListComments;

/// <summary>
/// Returns a request's comment thread, oldest first. Verifies the request exists <b>and is in the
/// caller's read scope</b> first (returns <c>null</c> → 404 otherwise) — the thread is scoped to
/// the parent request, so a caller can't read the discussion on a request they can't see.
/// </summary>
public sealed class ListCommentsHandler(IRequestRepository requests, ICommentRepository comments)
{
    public async Task<IReadOnlyList<CommentDto>?> HandleAsync(ListCommentsQuery query, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(query.RequestId, cancellationToken);
        if (request is null || RequestReadScope.IsOutOfScope(request, query.RequesterId, query.DepartmentId))
        {
            return null;
        }

        var all = await comments.ListByRequestAsync(query.RequestId, cancellationToken);
        return all.Select(CommentDto.FromDomain).ToList();
    }
}
