using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Assets;

/// <summary>Input for a parameterless asset lifecycle transition. ActorId is the IT Admin (JWT sub).</summary>
public sealed record AssetTransitionCommand(Guid AssetId, Guid ActorId);

public sealed class ReclaimAssetHandler(
    IAssetRepository assets,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AssetDto?> HandleAsync(AssetTransitionCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await assets.GetForUpdateAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = asset.Status;
        asset.Reclaim(now);
        auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetReclaimed, asset.Id, command.ActorId, fromStatus, asset.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssetDto.FromDomain(asset);
    }
}

public sealed class SendAssetToRepairHandler(
    IAssetRepository assets,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AssetDto?> HandleAsync(AssetTransitionCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await assets.GetForUpdateAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = asset.Status;
        asset.SendToRepair(now);
        auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetSentToRepair, asset.Id, command.ActorId, fromStatus, asset.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssetDto.FromDomain(asset);
    }
}

public sealed class ReturnAssetFromRepairHandler(
    IAssetRepository assets,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AssetDto?> HandleAsync(AssetTransitionCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await assets.GetForUpdateAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = asset.Status;
        asset.ReturnFromRepair(now);
        auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetReturnedFromRepair, asset.Id, command.ActorId, fromStatus, asset.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssetDto.FromDomain(asset);
    }
}

public sealed class RetireAssetHandler(
    IAssetRepository assets,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AssetDto?> HandleAsync(AssetTransitionCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await assets.GetForUpdateAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = asset.Status;
        asset.Retire(now);
        auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetRetired, asset.Id, command.ActorId, fromStatus, asset.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssetDto.FromDomain(asset);
    }
}
