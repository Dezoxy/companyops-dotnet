using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Port for persisting and reading <see cref="Request"/> aggregates. The concrete
/// EF Core implementation lives in Infrastructure — Application only depends on
/// this contract.
/// </summary>
public interface IRequestRepository
{
    void Add(Request request);

    /// <summary>Read-only load (no change tracking) for queries.</summary>
    Task<Request?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked load for a state transition (submit/approve/reject/fulfill), so changes
    /// to the aggregate are persisted on <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// The owned approval steps are loaded with the request.
    /// </summary>
    Task<Request?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scoped read of requests, newest first. At most one filter is set by the caller:
    /// <paramref name="requesterId"/> → only that requester's; <paramref name="departmentId"/> →
    /// only that department's; both null → all. The Api derives the scope from the principal's role.
    /// </summary>
    Task<IReadOnlyList<Request>> ListAsync(Guid? requesterId, Guid? departmentId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total number of requests in the same scope as <see cref="ListAsync"/> (same filters, no
    /// paging) — for the list's pagination total.
    /// </summary>
    Task<int> CountAsync(Guid? requesterId, Guid? departmentId, CancellationToken cancellationToken = default);
}
