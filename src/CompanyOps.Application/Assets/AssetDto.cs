using CompanyOps.Domain.Assets;

namespace CompanyOps.Application.Assets;

/// <summary>Read model for an asset, returned across the Application boundary.</summary>
public sealed record AssetDto(
    Guid Id,
    string Tag,
    string Name,
    AssetType Type,
    AssetStatus Status,
    Guid? AssignedToId,
    DateTimeOffset CreatedAtUtc)
{
    public static AssetDto FromDomain(Asset asset) =>
        new(asset.Id, asset.Tag, asset.Name, asset.Type, asset.Status, asset.AssignedToId, asset.CreatedAtUtc);
}
