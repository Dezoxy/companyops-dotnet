using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Integrations;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// Read-only operational snapshot of the integration pipeline (Phase 19), over the outbox and
/// processed-message tables (ADR 0007/0008). Counts are computed in the database; the recent list
/// projects only the columns the status view needs (never the payload). AsNoTracking throughout.
/// A few small count queries per call — fine for an infrequently-polled ops view.
/// </summary>
internal sealed class IntegrationStatusStore(AppDbContext dbContext) : IIntegrationStatusStore
{
    private const int RecentLimit = 50;
    private const int MaxErrorLength = 200;

    public async Task<IntegrationStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var outbox = dbContext.Set<OutboxMessage>().AsNoTracking();

        var published = await outbox.CountAsync(m => m.ProcessedAtUtc != null, cancellationToken);
        var failed = await outbox.CountAsync(m => m.ProcessedAtUtc == null && m.Error != null, cancellationToken);
        var pending = await outbox.CountAsync(m => m.ProcessedAtUtc == null && m.Error == null, cancellationToken);
        var processedByWorker = await dbContext.Set<ProcessedMessage>().AsNoTracking().CountAsync(cancellationToken);

        var recent = await outbox
            .OrderByDescending(m => m.OccurredAtUtc)
            .Take(RecentLimit)
            .Select(m => new
            {
                m.Id,
                m.Type,
                m.OccurredAtUtc,
                m.ProcessedAtUtc,
                m.Attempts,
                m.Error,
            })
            .ToListAsync(cancellationToken);

        var messages = recent
            .Select(m => new IntegrationMessageDto(
                m.Id,
                m.Type,
                StatusOf(m.ProcessedAtUtc, m.Error),
                m.OccurredAtUtc,
                m.ProcessedAtUtc,
                m.Attempts,
                Truncate(m.Error)))
            .ToList();

        return new IntegrationStatusDto(
            new OutboxSummaryDto(published + failed + pending, pending, published, failed),
            processedByWorker,
            messages);
    }

    // Published once the relay stamps it; Failed while a publish error is recorded (and not yet
    // published); Pending otherwise. Mirrors OutboxRelay's MarkProcessed/MarkFailed.
    private static string StatusOf(DateTimeOffset? processedAtUtc, string? error) =>
        processedAtUtc is not null ? "Published"
        : error is not null ? "Failed"
        : "Pending";

    // Defence in depth: cap the relay's error text before it leaves the server. The audience is
    // trusted (IT Admin / Auditor), but a publish exception message could be long or echo internal
    // detail, and it surfaces in the browser. The relay stores ex.Message (not the full trace).
    private static string? Truncate(string? error) =>
        error is { Length: > MaxErrorLength } ? string.Concat(error.AsSpan(0, MaxErrorLength), "…") : error;
}
