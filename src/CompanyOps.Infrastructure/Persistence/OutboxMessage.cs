namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// A pending integration event, written to the database in the same transaction as the
/// state change that produced it (the transactional outbox, ADR 0007). The relay
/// publishes unprocessed rows to the broker and stamps <see cref="ProcessedAtUtc"/>.
/// <see cref="Id"/> doubles as the message id / dedup key for at-least-once consumers.
/// This is an Infrastructure persistence concern, not a domain concept.
/// </summary>
internal sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(string type, string payload, DateTimeOffset occurredAtUtc)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        Error = error;
    }
}
