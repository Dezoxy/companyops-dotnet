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

    Task<Request?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Request>> ListAsync(CancellationToken cancellationToken = default);
}
