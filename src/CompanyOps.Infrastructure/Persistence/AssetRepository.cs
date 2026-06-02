using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Assets;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

internal sealed class AssetRepository(AppDbContext dbContext) : IAssetRepository
{
    public void Add(Asset asset) => dbContext.Assets.Add(asset);

    public Task<bool> TagExistsAsync(string tag, CancellationToken cancellationToken = default) =>
        dbContext.Assets.AsNoTracking().AnyAsync(a => a.Tag == tag, cancellationToken);

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Asset?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Asset>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        await dbContext.Assets
            .AsNoTracking()
            // Tie-break on Id so paging is deterministic when CreatedAtUtc ties.
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        dbContext.Assets.AsNoTracking().CountAsync(cancellationToken);
}
