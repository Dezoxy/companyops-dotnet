using CompanyOps.Application.Abstractions;

namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// Default <see cref="IAuditContext"/> for hosts with no HTTP request (the Worker, the migrator):
/// audits carry no source IP. The API replaces this with an HttpContext-backed implementation.
/// </summary>
internal sealed class NullAuditContext : IAuditContext
{
    public string? SourceIp => null;
}
