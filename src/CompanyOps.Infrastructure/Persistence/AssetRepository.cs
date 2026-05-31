using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Assets;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

internal sealed class AssetRepository(AppDbContext dbContext) : IAssetRepository
{
    public void Add(Asset asset) => dbContext.Assets.Add(asset);

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Asset?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Asset>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Assets
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
