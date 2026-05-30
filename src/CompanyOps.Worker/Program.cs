using CompanyOps.Infrastructure;
using CompanyOps.Worker;

var builder = Host.CreateApplicationBuilder(args);

// The Worker now does real work: DB (audit + idempotency), the broker, and the
// resilient external-system gateways (ADR 0008). It does NOT register the outbox relay
// (that's API-only, so the outbox isn't published twice).
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExternalSystems(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<INotificationSimulator, LoggingNotificationSimulator>();
builder.Services.AddScoped<IntegrationEventProcessor>();
builder.Services.AddHostedService<IntegrationEventConsumer>();

var host = builder.Build();
host.Run();
