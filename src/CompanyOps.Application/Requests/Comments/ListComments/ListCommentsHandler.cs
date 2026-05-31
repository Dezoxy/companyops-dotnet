using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Requests.Comments.ListComments;

/// <summary>
/// Returns a request's comment thread, oldest first. Verifies the request exists first
/// (returns <c>null</c> → 404 otherwise), so the thread endpoint is consistent with the
/// rest of the request endpoints rather than returning an empty list for a phantom id.
/// </summary>
public sealed class ListCommentsHandler(IRequestRepository requests, ICommentRepository comments)
{
    public async Task<IReadOnlyList<CommentDto>?> HandleAsync(ListCommentsQuery query, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(query.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var all = await comments.ListByRequestAsync(query.RequestId, cancellationToken);
        return all.Select(CommentDto.FromDomain).ToList();
    }
}
