using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Common;

namespace CompanyOps.Application.Assets;

public sealed record ListAssetsQuery(PageRequest? Page = null);

public sealed class ListAssetsHandler(IAssetRepository assets)
{
    public async Task<IReadOnlyList<AssetDto>> HandleAsync(ListAssetsQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page ?? new PageRequest();
        var all = await assets.ListAsync(page.Skip, page.Take, cancellationToken);
        return all.Select(AssetDto.FromDomain).ToList();
    }
}

public sealed record GetAssetByIdQuery(Guid AssetId);

public sealed class GetAssetByIdHandler(IAssetRepository assets)
{
    public async Task<AssetDto?> HandleAsync(GetAssetByIdQuery query, CancellationToken cancellationToken = default)
    {
        var asset = await assets.GetByIdAsync(query.AssetId, cancellationToken);
        return asset is null ? null : AssetDto.FromDomain(asset);
    }
}
