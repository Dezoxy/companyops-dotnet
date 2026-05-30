using CompanyOps.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CompanyOps.Api.Observability;

/// <summary>
/// Health checks + OpenTelemetry for the API. Metrics/traces export via OTLP when an
/// endpoint is configured (a collector arrives in Phase 11); otherwise they print to the
/// console in Development. Postgres and RabbitMQ emit their own OTel sources, so no
/// prerelease instrumentation packages are needed.
/// </summary>
public static class ObservabilitySetup
{
    private const string ServiceName = "CompanyOps.Api";

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        // The actual DB/RabbitMQ probes live in Infrastructure (next to the types they
        // check); the API only owns the HTTP surface (MapHealthEndpoints below).
        services.AddHealthChecks().AddInfrastructureHealthChecks();

        var otlp = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Npgsql")
                    // Wildcard: OTel matches it against the broker's "RabbitMQ.Client"
                    // ActivitySource (and survives a future split into .Publisher/.Subscriber).
                    .AddSource("RabbitMQ.Client*");

                if (otlp)
                {
                    tracing.AddOtlpExporter();
                }
                else if (isDevelopment)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Npgsql");

                if (otlp)
                {
                    metrics.AddOtlpExporter();
                }
                else if (isDevelopment)
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }

    /// <summary>Maps liveness (<c>/health</c>, no dependency checks) and readiness
    /// (<c>/health/ready</c>, the dependency checks). Both are anonymous.</summary>
    public static void MapHealthEndpoints(this WebApplication app)
    {
        // AllowAnonymous: health probes are infrastructure traffic, never tokened — and
        // this stays correct if Phase 11 sets a default auth policy that requires a user.
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
            .AllowAnonymous();
    }
}
