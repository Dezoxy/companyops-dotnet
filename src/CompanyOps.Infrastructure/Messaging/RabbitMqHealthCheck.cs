using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CompanyOps.Infrastructure.Messaging;

/// <summary>
/// Readiness probe for the broker: opens (and disposes) a channel on the shared
/// connection. Lives in Infrastructure next to <see cref="RabbitMqConnection"/> so the
/// knowledge of how to probe the broker stays with the type that owns it — the API just
/// exposes the result over HTTP. Registered via <c>AddInfrastructureHealthChecks</c>.
/// </summary>
internal sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unreachable.", ex);
        }
    }
}
