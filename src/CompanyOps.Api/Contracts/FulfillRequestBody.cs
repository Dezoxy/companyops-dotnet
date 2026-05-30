namespace CompanyOps.Api.Contracts;

/// <summary>
/// Body for <c>POST /requests/{id}/fulfill</c>. <see cref="ActorId"/> records who
/// fulfilled it — in the body only until Phase 3 supplies the authenticated principal.
/// </summary>
public sealed record FulfillRequestBody(Guid ActorId);
