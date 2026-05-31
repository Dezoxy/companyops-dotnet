namespace CompanyOps.Application.Integrations;

/// <summary>
/// Health of the transactional outbox (the producer/relay side, ADR 0007): how many integration
/// messages are awaiting relay, have been published to the broker, or are failing to publish.
/// </summary>
public sealed record OutboxSummaryDto(int Total, int Pending, int Published, int Failed);

/// <summary>
/// One outbox message for the recent-activity list. <see cref="Status"/> is derived: <c>Published</c>
/// once relayed, <c>Failed</c> while a publish error is recorded, else <c>Pending</c>. The payload is
/// intentionally omitted — the status view never needs (or should expose) the event body.
/// </summary>
public sealed record IntegrationMessageDto(
    Guid Id,
    string Type,
    string Status,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? ProcessedAtUtc,
    int Attempts,
    string? Error);

/// <summary>
/// A snapshot of the async integration pipeline (Phase 19): the outbox summary, how many messages
/// the Worker has consumed (the dedup/idempotency markers, ADR 0008), and the most recent messages.
/// </summary>
public sealed record IntegrationStatusDto(
    OutboxSummaryDto Outbox,
    int ProcessedByWorker,
    IReadOnlyList<IntegrationMessageDto> Recent);
