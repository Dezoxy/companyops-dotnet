using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Read port for the audit trail (the write side is <see cref="IAuditLogger"/>). Read-only
/// by design — there is no update/delete path anywhere, keeping the log append-only.
/// </summary>
public interface IAuditLogReader
{
    Task<IReadOnlyList<AuditLog>> ListAsync(CancellationToken cancellationToken = default);
}
