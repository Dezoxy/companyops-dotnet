using CompanyOps.Application.Assets;
using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Auditing;
using Xunit;

namespace CompanyOps.Application.Tests;

/// <summary>
/// Fast Application-layer tests for the asset-console handlers — the orchestration (load → domain
/// transition → audit (from→to) → save, and null-on-missing) that was previously only exercised
/// through the Postgres integration path. Fakes, no database; mirrors <see cref="RequestHandlerTests"/>.
/// </summary>
public class AssetHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ItAdmin = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Holder = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly FakeAssetRepository _assets = new();
    private readonly CapturingAuditLogger _audit = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FixedTimeProvider _clock = new(Now);

    private Asset Seed(Action<Asset>? advance = null)
    {
        var asset = Asset.Register("AST-1", "MacBook Pro", AssetType.Laptop, Now);
        advance?.Invoke(asset);
        _assets.Seed(asset);
        return asset;
    }

    [Fact]
    public async Task Register_AddsInStockAsset_AuditsRegistered_AndSaves()
    {
        var handler = new RegisterAssetHandler(_assets, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new RegisterAssetCommand("AST-NEW", "Dell XPS", AssetType.Laptop, ItAdmin));

        Assert.NotNull(dto);
        var stored = Assert.Single(_assets.Store.Values);
        Assert.Equal(AssetStatus.InStock, stored.Status);
        Assert.Equal(1, _uow.SaveCount);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.AssetRegistered && e.TargetId == stored.Id);
    }

    [Fact]
    public async Task Assign_TransitionsToAssignedHolder_AuditsFromToStatus()
    {
        var asset = Seed();
        var handler = new AssignAssetHandler(_assets, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new AssignAssetCommand(asset.Id, Holder, ItAdmin));

        Assert.NotNull(dto);
        Assert.Equal(AssetStatus.Assigned, asset.Status);
        Assert.Equal(Holder, asset.AssignedToId); // the holder, not the acting IT Admin
        var entry = Assert.Single(_audit.Entries, e => e.Action == AuditAction.AssetAssigned);
        Assert.Equal(asset.Id, entry.TargetId);
        Assert.Equal(ItAdmin, entry.ActorId);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Reclaim_TransitionsAssignedToInStock_AuditsReclaimed()
    {
        var asset = Seed(a => a.Assign(Holder, Now));
        var handler = new ReclaimAssetHandler(_assets, _audit, _uow, _clock);

        await handler.HandleAsync(new AssetTransitionCommand(asset.Id, ItAdmin));

        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.AssetReclaimed && e.TargetId == asset.Id);
    }

    [Fact]
    public async Task SendToRepair_TransitionsToInRepair_AuditsSentToRepair()
    {
        var asset = Seed();
        var handler = new SendAssetToRepairHandler(_assets, _audit, _uow, _clock);

        await handler.HandleAsync(new AssetTransitionCommand(asset.Id, ItAdmin));

        Assert.Equal(AssetStatus.InRepair, asset.Status);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.AssetSentToRepair && e.TargetId == asset.Id);
    }

    [Fact]
    public async Task ReturnFromRepair_TransitionsToInStock_AuditsReturned()
    {
        var asset = Seed(a => a.SendToRepair(Now));
        var handler = new ReturnAssetFromRepairHandler(_assets, _audit, _uow, _clock);

        await handler.HandleAsync(new AssetTransitionCommand(asset.Id, ItAdmin));

        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.AssetReturnedFromRepair && e.TargetId == asset.Id);
    }

    [Fact]
    public async Task Retire_TransitionsToRetired_AuditsRetired()
    {
        var asset = Seed();
        var handler = new RetireAssetHandler(_assets, _audit, _uow, _clock);

        await handler.HandleAsync(new AssetTransitionCommand(asset.Id, ItAdmin));

        Assert.Equal(AssetStatus.Retired, asset.Status);
        Assert.Contains(_audit.Entries, e => e.Action == AuditAction.AssetRetired && e.TargetId == asset.Id);
    }

    [Fact]
    public async Task Assign_MissingAsset_ReturnsNull_NoAuditNoSave()
    {
        var handler = new AssignAssetHandler(_assets, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new AssignAssetCommand(Guid.NewGuid(), Holder, ItAdmin));

        Assert.Null(dto);
        Assert.Empty(_audit.Entries);
        Assert.Equal(0, _uow.SaveCount);
    }

    [Fact]
    public async Task Transition_MissingAsset_ReturnsNull_NoAuditNoSave()
    {
        var handler = new RetireAssetHandler(_assets, _audit, _uow, _clock);

        var dto = await handler.HandleAsync(new AssetTransitionCommand(Guid.NewGuid(), ItAdmin));

        Assert.Null(dto);
        Assert.Empty(_audit.Entries);
        Assert.Equal(0, _uow.SaveCount);
    }
}
