using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Assets;

/// <summary>
/// One entry in an asset's history — an audit record for the asset, newest first.
/// <see cref="AffectedUserId"/> is who held it (the assignee on assign, the prior holder on
/// reclaim / send-to-repair / retire); null where the action concerns no holder.
/// </summary>
public sealed record AssetHistoryEntryDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid ActorId,
    AuditAction Action,
    string? FromStatus,
    string? ToStatus,
    Guid? AffectedUserId)
{
    public static AssetHistoryEntryDto FromDomain(AuditLog log) =>
        new(log.Id, log.OccurredAtUtc, log.ActorId, log.Action, log.FromStatus, log.ToStatus, log.AffectedUserId);
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
