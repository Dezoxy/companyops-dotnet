using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Assets;

/// <summary>One entry in an asset's history — an audit record for the asset, newest first.</summary>
public sealed record AssetHistoryEntryDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid ActorId,
    AuditAction Action,
    string? FromStatus,
    string? ToStatus)
{
    public static AssetHistoryEntryDto FromDomain(AuditLog log) =>
        new(log.Id, log.OccurredAtUtc, log.ActorId, log.Action, log.FromStatus, log.ToStatus);
}

public sealed record GetAssetHistoryQuery(Guid AssetId);

public sealed class GetAssetHistoryHandler(IAssetRepository assets, IAuditLogReader auditReader)
{
    public async Task<IReadOnlyList<AssetHistoryEntryDto>?> HandleAsync(
        GetAssetHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var asset = await assets.GetByIdAsync(query.AssetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var entries = await auditReader.ListForTargetAsync("Asset", query.AssetId, cancellationToken);
        return entries.Select(AssetHistoryEntryDto.FromDomain).ToList();
    }
}
