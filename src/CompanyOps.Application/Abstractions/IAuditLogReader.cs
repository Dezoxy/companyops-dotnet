using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Read port for the audit trail (the write side is <see cref="IAuditLogger"/>). Read-only
/// by design — there is no update/delete path anywhere, keeping the log append-only.
/// </summary>
public interface IAuditLogReader
{
    Task<IReadOnlyList<AuditLog>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Total number of audit entries (no paging) — for the trail's pagination total.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Audit entries for one target object (e.g. an asset's history), newest first.</summary>
    Task<IReadOnlyList<AuditLog>> ListForTargetAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default);
}
