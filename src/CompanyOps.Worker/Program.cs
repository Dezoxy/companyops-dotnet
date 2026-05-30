using CompanyOps.Infrastructure;
using CompanyOps.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Structured logging (JSON to console), matching the API. There's no per-request scope
// here, but the RabbitMQ consume span sets Activity.Current, so Serilog stamps each
// message's logs with the trace's TraceId/SpanId — the same TraceId the API logged when
// it published the event, which is what links the two services' logs.
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

// The Worker does real work: DB (audit + idempotency), the broker, and the resilient
// external-system gateways (ADR 0008). It does NOT register the outbox relay (that's
// API-only, so the outbox isn't published twice).
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExternalSystems(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<INotificationSimulator, LoggingNotificationSimulator>();
builder.Services.AddScoped<IntegrationEventProcessor>();
builder.Services.AddHostedService<IntegrationEventConsumer>();
builder.Services.AddWorkerObservability(builder.Configuration, builder.Environment.IsDevelopment());

var host = builder.Build();
host.Run();
