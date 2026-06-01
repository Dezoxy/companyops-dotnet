using CompanyOps.Domain.Assets;

namespace CompanyOps.Api.Contracts;

/// <summary>Body for <c>POST /assets</c> — register a new asset into inventory.</summary>
/// <remarks><c>Type</c> is nullable so an omitted asset type is rejected by the validator
/// rather than silently defaulting to <c>Laptop</c>.</remarks>
public sealed record RegisterAssetRequest(string Tag, string Name, AssetType? Type);
