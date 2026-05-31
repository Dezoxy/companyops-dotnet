using CompanyOps.Domain.Common;

namespace CompanyOps.Domain.Assets;

/// <summary>
/// A company asset (laptop, phone, licence, …) tracked through its lifecycle. The aggregate
/// owns the state machine: <see cref="AssetStatus.InStock"/> → <see cref="AssetStatus.Assigned"/>
/// → (reclaim) → InStock, with <see cref="AssetStatus.InRepair"/> and
/// <see cref="AssetStatus.Retired"/> as branches. Illegal transitions throw
/// <see cref="DomainException"/>. Who held it / when it changed is recorded in the audit trail
/// by the handlers (the asset's history), not on the aggregate.
/// </summary>
public sealed class Asset
{
    public const int TagMaxLength = 50;
    public const int NameMaxLength = 200;

    public Guid Id { get; private set; }
    public string Tag { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public AssetType Type { get; private set; }
    public AssetStatus Status { get; private set; }

    /// <summary>The user the asset is currently assigned to; null unless <see cref="AssetStatus.Assigned"/>.</summary>
    public Guid? AssignedToId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Required by EF Core's materializer; not for application use.
    private Asset()
    {
    }

    private Asset(Guid id, string tag, string name, AssetType type, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Tag = tag;
        Name = name;
        Type = type;
        Status = AssetStatus.InStock;
        AssignedToId = null;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Register a new asset into the inventory (starts <see cref="AssetStatus.InStock"/>).</summary>
    public static Asset Register(string tag, string name, AssetType type, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new DomainException("Asset tag is required.");
        }

        tag = tag.Trim();
        if (tag.Length > TagMaxLength)
        {
            throw new DomainException($"Asset tag must be at most {TagMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Asset name is required.");
        }

        name = name.Trim();
        if (name.Length > NameMaxLength)
        {
            throw new DomainException($"Asset name must be at most {NameMaxLength} characters.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException($"Unknown asset type '{type}'.");
        }

        return new Asset(Guid.NewGuid(), tag, name, type, nowUtc);
    }

    /// <summary>Assign an in-stock asset to a user: <c>InStock → Assigned</c>.</summary>
    public void Assign(Guid userId, DateTimeOffset nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("An assignment must record the user.");
        }

        if (Status != AssetStatus.InStock)
        {
            throw new DomainException($"Only an in-stock asset can be assigned; this asset is {Status}.");
        }

        _ = nowUtc;
        Status = AssetStatus.Assigned;
        AssignedToId = userId;
    }

    /// <summary>Reclaim an assigned asset back into stock: <c>Assigned → InStock</c>.</summary>
    public void Reclaim(DateTimeOffset nowUtc)
    {
        if (Status != AssetStatus.Assigned)
        {
            throw new DomainException($"Only an assigned asset can be reclaimed; this asset is {Status}.");
        }

        _ = nowUtc;
        Status = AssetStatus.InStock;
        AssignedToId = null;
    }

    /// <summary>Send an asset for repair: <c>InStock | Assigned → InRepair</c> (clears the holder).</summary>
    public void SendToRepair(DateTimeOffset nowUtc)
    {
        if (Status is not (AssetStatus.InStock or AssetStatus.Assigned))
        {
            throw new DomainException($"Only an in-stock or assigned asset can be sent for repair; this asset is {Status}.");
        }

        _ = nowUtc;
        Status = AssetStatus.InRepair;
        AssignedToId = null;
    }

    /// <summary>Return a repaired asset to stock: <c>InRepair → InStock</c>.</summary>
    public void ReturnFromRepair(DateTimeOffset nowUtc)
    {
        if (Status != AssetStatus.InRepair)
        {
            throw new DomainException($"Only an asset in repair can be returned to stock; this asset is {Status}.");
        }

        _ = nowUtc;
        Status = AssetStatus.InStock;
    }

    /// <summary>Retire an asset out of service (terminal): <c>* → Retired</c>.</summary>
    public void Retire(DateTimeOffset nowUtc)
    {
        if (Status == AssetStatus.Retired)
        {
            throw new DomainException("This asset is already retired.");
        }

        _ = nowUtc;
        Status = AssetStatus.Retired;
        AssignedToId = null;
    }
}
