namespace CompanyOps.Domain.Assets;

/// <summary>Broad category of an asset. Kept coarse — full CMDB classification is out of scope.</summary>
public enum AssetType
{
    Laptop = 0,
    Desktop = 1,
    Mobile = 2,
    Monitor = 3,
    Peripheral = 4,
    Software = 5,
    Other = 6,
}
