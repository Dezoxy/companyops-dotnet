using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// Audit trail persistence. <see cref="Add"/> enlists an entry in the current
/// <see cref="AppDbContext"/> so it commits in the same transaction as the state change
/// (via the shared unit of work); <see cref="ListAsync"/> is a read-only query. No update
/// or delete path is exposed — the log is append-only.
/// </summary>
internal sealed class AuditLogStore(AppDbContext dbContext) : IAuditLogger, IAuditLogReader
{
    public void Add(AuditLog entry) => dbContext.AuditLogs.Add(entry);

    // Cap the read so an unbounded, ever-growing table can't be pulled in one request.
    // Cursor/limit pagination is the proper follow-up (see ListAuditLogsQuery).
    private const int MaxRows = 500;

    public async Task<IReadOnlyList<AuditLog>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> ListForTargetAsync(
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default) =>
        await dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.TargetType == targetType && a.TargetId == targetId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);
}
