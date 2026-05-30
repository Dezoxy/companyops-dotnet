namespace CompanyOps.Application.IntegrationEvents;

/// <summary>
/// Marker for an integration event — a fact published to other processes (the Worker)
/// over the message bus. Integration events carry ids and essentials only (never EF
/// entities), so producer and consumer evolve independently. The event's type name is
/// the routing key on the outbox/bus.
/// </summary>
public interface IIntegrationEvent;
