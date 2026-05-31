using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Assets;

/// <summary>Register a new asset into inventory. ActorId is the authenticated IT Admin (JWT sub).</summary>
public sealed record RegisterAssetCommand(string Tag, string Name, AssetType Type, Guid ActorId);

public sealed class RegisterAssetHandler(
    IAssetRepository assets,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AssetDto> HandleAsync(RegisterAssetCommand command, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var asset = Asset.Register(command.Tag, command.Name, command.Type, now);
        assets.Add(asset);
        auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetRegistered, asset.Id, command.ActorId, null, asset.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssetDto.FromDomain(asset);
    }
}
