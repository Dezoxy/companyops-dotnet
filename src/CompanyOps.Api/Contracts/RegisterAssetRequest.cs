using CompanyOps.Domain.Assets;

namespace CompanyOps.Api.Contracts;

/// <summary>Body for <c>POST /assets</c> — register a new asset into inventory.</summary>
public sealed record RegisterAssetRequest(string Tag, string Name, AssetType Type);
