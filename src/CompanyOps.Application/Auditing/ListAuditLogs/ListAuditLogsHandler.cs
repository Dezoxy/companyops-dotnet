using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Common;

namespace CompanyOps.Application.Auditing.ListAuditLogs;

/// <summary>Returns the audit trail (newest first), paged. Read-only.</summary>
public sealed class ListAuditLogsHandler(IAuditLogReader auditLogs)
{
    public async Task<PagedResult<AuditLogDto>> HandleAsync(ListAuditLogsQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page ?? new PageRequest();
        var logs = await auditLogs.ListAsync(page.Skip, page.Take, cancellationToken);
        var total = await auditLogs.CountAsync(cancellationToken);
        var dtos = logs.Select(AuditLogDto.FromDomain).ToList();
        return new PagedResult<AuditLogDto>(dtos, total, page.Page, page.PageSize);
    }
}
