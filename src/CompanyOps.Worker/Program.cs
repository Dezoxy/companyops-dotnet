using CompanyOps.Infrastructure;
using CompanyOps.Worker;

var builder = Host.CreateApplicationBuilder(args);

// The Worker needs the broker but not the database (Phase 5 simulates notifications).
builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddSingleton<INotificationSimulator, LoggingNotificationSimulator>();
builder.Services.AddHostedService<RequestApprovedConsumer>();

var host = builder.Build();
host.Run();
