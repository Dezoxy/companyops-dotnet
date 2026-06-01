using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Common;

namespace CompanyOps.Application.Auditing.ListAuditLogs;

/// <summary>Returns the audit trail (newest first), paged. Read-only.</summary>
public sealed class ListAuditLogsHandler(IAuditLogReader auditLogs)
{
    public async Task<IReadOnlyList<AuditLogDto>> HandleAsync(ListAuditLogsQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page ?? new PageRequest();
        var logs = await auditLogs.ListAsync(page.Skip, page.Take, cancellationToken);
        return [.. logs.Select(AuditLogDto.FromDomain)];
    }
}
