namespace CompanyOps.Api.Contracts;

/// <summary>
/// Body for <c>POST /requests/{id}/fulfill</c>. For an asset-lifecycle request, IT names the
/// in-stock <c>AssignedAssetId</c> to assign to the requester; for every other type the body is
/// omitted or empty (the Domain rejects a stray asset id). The fulfiller's identity comes from
/// the authenticated principal, never the body.
/// </summary>
public sealed record FulfillRequestBody(Guid? AssignedAssetId);
