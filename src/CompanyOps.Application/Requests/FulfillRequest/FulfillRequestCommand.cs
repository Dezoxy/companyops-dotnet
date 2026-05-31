namespace CompanyOps.Application.Requests.FulfillRequest;

/// <summary>
/// Use-case input for fulfilling an approved request. Synchronous for now; the Phase 5
/// worker takes this over asynchronously.
/// </summary>
/// <remarks>
/// <paramref name="ActorId"/> records who fulfilled it — derived from the authenticated
/// principal in the Api, never the request body.
/// <paramref name="AssignedAssetId"/> is the in-stock asset IT assigns to the requester when
/// fulfilling an asset-lifecycle request; it must be null for every other type (enforced in
/// the Domain).
/// </remarks>
public sealed record FulfillRequestCommand(Guid RequestId, Guid ActorId, Guid? AssignedAssetId = null);
