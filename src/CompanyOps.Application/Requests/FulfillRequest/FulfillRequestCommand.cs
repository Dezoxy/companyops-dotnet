namespace CompanyOps.Application.Requests.FulfillRequest;

/// <summary>
/// Use-case input for fulfilling an approved request. Synchronous for now; the Phase 5
/// worker takes this over asynchronously.
/// </summary>
/// <remarks>
/// <paramref name="ActorId"/> records who fulfilled it — the pre-auth bridge, replaced
/// by the authenticated principal in Phase 3.
/// </remarks>
public sealed record FulfillRequestCommand(Guid RequestId, Guid ActorId);
