namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// Marker that an integration message (by its outbox/message id) has been handled, so
/// at-least-once redeliveries are skipped. Infrastructure plumbing, not a domain concept.
/// </summary>
internal sealed class ProcessedMessage
{
    public Guid Id { get; private set; }
    public DateTimeOffset ProcessedAtUtc { get; private set; }

    private ProcessedMessage()
    {
    }

    public ProcessedMessage(Guid id, DateTimeOffset processedAtUtc)
    {
        Id = id;
        ProcessedAtUtc = processedAtUtc;
    }
}
