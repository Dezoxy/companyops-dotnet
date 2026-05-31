using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Common;

namespace CompanyOps.Domain.Tests.Assets;

/// <summary>The <see cref="Asset"/> lifecycle state machine: register → assign → reclaim, with
/// repair and retire branches. Illegal transitions throw <see cref="DomainException"/>.</summary>
public class AssetTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Holder = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Asset NewAsset() => Asset.Register("AST-001", "MacBook Pro 16", AssetType.Laptop, Now);

    [Fact]
    public void Register_StartsInStock_Unassigned()
    {
        var asset = NewAsset();

        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.Equal("AST-001", asset.Tag);
        Assert.Equal(AssetType.Laptop, asset.Type);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Null(asset.AssignedToId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankTag_ThrowsDomainException(string tag)
    {
        Assert.Throws<DomainException>(() => Asset.Register(tag, "Laptop", AssetType.Laptop, Now));
    }

    [Fact]
    public void Register_WithBlankName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Asset.Register("AST-001", "  ", AssetType.Laptop, Now));
    }

    [Fact]
    public void Assign_FromInStock_SetsAssignedToHolder()
    {
        var asset = NewAsset();

        asset.Assign(Holder, Now);

        Assert.Equal(AssetStatus.Assigned, asset.Status);
        Assert.Equal(Holder, asset.AssignedToId);
    }

    [Fact]
    public void Assign_WhenAlreadyAssigned_ThrowsDomainException()
    {
        var asset = NewAsset();
        asset.Assign(Holder, Now);

        Assert.Throws<DomainException>(() => asset.Assign(Holder, Now));
    }

    [Fact]
    public void Assign_WithEmptyUser_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewAsset().Assign(Guid.Empty, Now));
    }

    [Fact]
    public void Reclaim_FromAssigned_ReturnsToStockAndClearsHolder()
    {
        var asset = NewAsset();
        asset.Assign(Holder, Now);

        asset.Reclaim(Now);

        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Null(asset.AssignedToId);
    }

    [Fact]
    public void Reclaim_WhenInStock_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewAsset().Reclaim(Now));
    }

    [Fact]
    public void SendToRepair_FromAssigned_SetsInRepairAndClearsHolder()
    {
        var asset = NewAsset();
        asset.Assign(Holder, Now);

        asset.SendToRepair(Now);

        Assert.Equal(AssetStatus.InRepair, asset.Status);
        Assert.Null(asset.AssignedToId);
    }

    [Fact]
    public void ReturnFromRepair_SetsInStock()
    {
        var asset = NewAsset();
        asset.SendToRepair(Now);

        asset.ReturnFromRepair(Now);

        Assert.Equal(AssetStatus.InStock, asset.Status);
    }

    [Fact]
    public void ReturnFromRepair_WhenNotInRepair_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewAsset().ReturnFromRepair(Now));
    }

    [Fact]
    public void Retire_FromAssigned_SetsRetiredAndClearsHolder()
    {
        var asset = NewAsset();
        asset.Assign(Holder, Now);

        asset.Retire(Now);

        Assert.Equal(AssetStatus.Retired, asset.Status);
        Assert.Null(asset.AssignedToId);
    }

    [Fact]
    public void Retire_WhenAlreadyRetired_ThrowsDomainException()
    {
        var asset = NewAsset();
        asset.Retire(Now);

        Assert.Throws<DomainException>(() => asset.Retire(Now));
    }

    [Fact]
    public void Assign_AfterRetired_ThrowsDomainException()
    {
        var asset = NewAsset();
        asset.Retire(Now);

        Assert.Throws<DomainException>(() => asset.Assign(Holder, Now));
    }
}
