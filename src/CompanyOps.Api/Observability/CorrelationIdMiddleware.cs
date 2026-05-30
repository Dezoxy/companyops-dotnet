using System.Text.RegularExpressions;
using Serilog.Context;

namespace CompanyOps.Api.Observability;

/// <summary>
/// Assigns each request a correlation id (honouring a <em>well-formed</em> inbound
/// <c>X-Correlation-ID</c> header, else generating one), echoes it back on the response,
/// and pushes it into the Serilog log context so every log line for the request carries
/// it. This is also the seam for propagating correlation onward (to the outbox/worker)
/// and into audit later.
/// </summary>
/// <remarks>
/// The inbound value is client-controlled, so it's only accepted when it matches a strict
/// allowlist (short, alphanumeric + hyphen). That keeps an attacker from injecting control
/// characters into a response header (header-splitting) or flooding the logs with an
/// arbitrary-length value; anything that fails the check is replaced with a fresh GUID.
/// </remarks>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var value)
            && WellFormed().IsMatch(value.ToString())
                ? value.ToString()
                : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    // Accept only short, safe ids (GUIDs and typical trace ids fit); reject everything else.
    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex WellFormed();
}
