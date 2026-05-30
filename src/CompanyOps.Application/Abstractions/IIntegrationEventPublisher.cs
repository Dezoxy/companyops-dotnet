using CompanyOps.Application.IntegrationEvents;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Enqueues an integration event for asynchronous delivery. The implementation writes
/// it to the outbox in the **same transaction** as the state change (committed by
/// <see cref="IUnitOfWork.SaveChangesAsync"/>), so an event is never lost relative to,
/// nor published without, the change that produced it (ADR 0007). A separate relay
/// publishes the outbox to the broker.
/// </summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
