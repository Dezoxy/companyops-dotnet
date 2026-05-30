namespace CompanyOps.Application.IntegrationEvents;

/// <summary>
/// Published when a request is fulfilled. The Worker reacts by reserving the asset in
/// the external Inventory system (ADR 0008).
/// </summary>
public sealed record RequestFulfilled(
    Guid RequestId,
    Guid ActorId,
    DateTimeOffset FulfilledAtUtc) : IIntegrationEvent;
