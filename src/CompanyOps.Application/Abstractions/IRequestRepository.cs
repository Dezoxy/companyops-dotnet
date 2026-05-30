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

    Task<IReadOnlyList<Request>> ListAsync(CancellationToken cancellationToken = default);
}
