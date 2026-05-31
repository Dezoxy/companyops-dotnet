using CompanyOps.Application.Abstractions;

namespace CompanyOps.Api.Auth;

/// <summary>
/// <see cref="IAuditContext"/> backed by the current HTTP request — records the caller's IP for
/// audit provenance. Behind the Traefik edge the real client IP comes from the ForwardedHeaders
/// middleware (it rewrites <c>RemoteIpAddress</c> from <c>X-Forwarded-For</c>).
/// </summary>
public sealed class HttpAuditContext(IHttpContextAccessor httpContextAccessor) : IAuditContext
{
    public string? SourceIp => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
