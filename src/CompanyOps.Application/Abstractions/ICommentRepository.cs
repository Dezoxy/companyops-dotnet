using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Port for persisting and reading <see cref="Comment"/>s. Comments are a separate aggregate
/// keyed by request id; the EF Core implementation lives in Infrastructure.
/// </summary>
public interface ICommentRepository
{
    void Add(Comment comment);

    /// <summary>All comments on a request, oldest first (thread order).</summary>
    Task<IReadOnlyList<Comment>> ListByRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
}
