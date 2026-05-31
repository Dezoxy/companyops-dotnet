using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Assets;

/// <summary>Assign an in-stock asset to a user. ActorId is the IT Admin; UserId is the holder.</summary>
public sealed record AssignAssetCommand(Guid AssetId, Guid UserId, Guid ActorId);

public sealed class AssignAssetHandler(
    IAssetRepository assets,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AssetDto?> HandleAsync(AssignAssetCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await assets.GetForUpdateAsync(command.AssetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = asset.Status;
        asset.Assign(command.UserId, now);
        // The audit records who held it: the assignee (command.UserId), distinct from the actor (IT Admin).
        auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetAssigned, asset.Id, command.ActorId, fromStatus, asset.Status, now, command.UserId));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssetDto.FromDomain(asset);
    }
}
