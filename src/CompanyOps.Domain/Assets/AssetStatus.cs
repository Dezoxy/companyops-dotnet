namespace CompanyOps.Domain.Assets;

/// <summary>
/// Lifecycle state of an <see cref="Asset"/>. Transitions are enforced by the aggregate
/// (illegal moves throw); the path is In stock → Assigned → (reclaim) → In stock, with
/// In repair and Retired as branches.
/// </summary>
public enum AssetStatus
{
    InStock = 0,
    Assigned = 1,
    InRepair = 2,
    Retired = 3,
}
