using CompanyOps.Domain.Assets;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Port for persisting and reading <see cref="Asset"/> aggregates. Mirrors
/// <see cref="IRequestRepository"/>: a no-tracking read for queries, a tracked load for
/// lifecycle transitions. The EF Core implementation lives in Infrastructure.
/// </summary>
public interface IAssetRepository
{
    void Add(Asset asset);

    /// <summary>True if an asset already carries this (already-normalized) tag — the uniqueness
    /// pre-check for registration. The unique index on the column is the integrity backstop.</summary>
    Task<bool> TagExistsAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>Read-only load (no change tracking) for queries.</summary>
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tracked load for a lifecycle transition, so the change persists on save.</summary>
    Task<Asset?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Asset>> ListAsync(CancellationToken cancellationToken = default);
}
