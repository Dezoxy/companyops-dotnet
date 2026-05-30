namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Idempotency guard for at-least-once message delivery (ADR 0007/0008): records which
/// integration-message ids have been handled so a redelivery doesn't repeat the side
/// effect. <see cref="MarkProcessed"/> enlists in the current unit of work so it commits
/// atomically with the work it guards.
/// </summary>
public interface IProcessedMessageGuard
{
    Task<bool> AlreadyProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    void MarkProcessed(Guid messageId, DateTimeOffset processedAtUtc);
}
