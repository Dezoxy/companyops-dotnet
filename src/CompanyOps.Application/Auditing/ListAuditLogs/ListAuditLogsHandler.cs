using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Auditing.ListAuditLogs;

/// <summary>Returns the audit trail (newest first). Read-only.</summary>
public sealed class ListAuditLogsHandler(IAuditLogReader auditLogs)
{
    public async Task<IReadOnlyList<AuditLogDto>> HandleAsync(ListAuditLogsQuery query, CancellationToken cancellationToken = default)
    {
        // query carries no filters in Phase 4.
        var logs = await auditLogs.ListAsync(cancellationToken);
        return [.. logs.Select(AuditLogDto.FromDomain)];
    }
}
