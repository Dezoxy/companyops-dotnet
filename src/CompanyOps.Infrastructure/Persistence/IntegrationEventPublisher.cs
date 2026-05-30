using System.Text.Json;
using CompanyOps.Application.Abstractions;
using CompanyOps.Application.IntegrationEvents;

namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// Writes an integration event to the outbox as part of the current unit of work, so it
/// commits atomically with the state change (ADR 0007). It does not touch the broker —
/// the relay publishes the row later.
/// </summary>
internal sealed class IntegrationEventPublisher(AppDbContext dbContext, TimeProvider timeProvider)
    : IIntegrationEventPublisher
{
    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType().Name;
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        dbContext.Set<OutboxMessage>().Add(new OutboxMessage(type, payload, timeProvider.GetUtcNow()));
    }
}
