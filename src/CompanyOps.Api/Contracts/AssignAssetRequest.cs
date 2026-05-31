namespace CompanyOps.Api.Contracts;

/// <summary>Body for <c>POST /assets/{id}/assign</c> — the user to assign the asset to.</summary>
public sealed record AssignAssetRequest(Guid UserId);
