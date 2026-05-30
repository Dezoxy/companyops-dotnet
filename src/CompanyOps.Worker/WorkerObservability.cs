using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CompanyOps.Worker;

/// <summary>
/// OpenTelemetry for the Worker: HTTP client (the external-system gateways), runtime,
/// Postgres (Npgsql), and RabbitMQ sources. The RabbitMQ consume span continues the trace
/// the API started when it published, so a request's API→worker hops share one TraceId —
/// and that TraceId shows up in the Serilog JSON on both sides, linking the logs.
/// Exports via OTLP when an endpoint is configured (a collector arrives in Phase 11);
/// otherwise prints to the console in Development. The Worker hosts no HTTP server, so it
/// has no health endpoints — its liveness is the process plus its broker connection.
/// </summary>
public static class WorkerObservability
{
    private const string ServiceName = "CompanyOps.Worker";

    public static IServiceCollection AddWorkerObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var otlp = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
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
}
