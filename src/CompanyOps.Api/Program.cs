using System.Text.Json.Serialization;
using CompanyOps.Api.Auth;
using CompanyOps.Api.ErrorHandling;
using CompanyOps.Api.Observability;
using CompanyOps.Application;
using CompanyOps.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Structured logging (JSON to console), enriched with the request correlation id.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

// One-shot migrator mode (the compose `migrator` service): register only what EF needs,
// apply migrations, and exit. Deliberately skips auth/controllers/observability so the
// migrator never requires their config — or that Keycloak is reachable — just to migrate.
if (args.Contains("--migrate"))
{
    builder.Services.AddSingleton(TimeProvider.System); // AddInfrastructure's graph needs it
    builder.Services.AddInfrastructure(builder.Configuration);
    var migrator = builder.Build();
    await migrator.Services.MigrateDatabaseAsync();
    return;
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Serialize enums as their names (e.g. "Procurement"), not integers.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// Map domain rule violations to RFC 7807 problem responses.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// TimeProvider.System makes "now" injectable and testable (no custom clock port).
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddCompanyOpsAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOutboxRelay(); // producer-side: publish the outbox to RabbitMQ
builder.Services.AddObservability(builder.Configuration, builder.Environment.IsDevelopment());

// Behind the Traefik edge (Phase 11) the API speaks HTTP; trust X-Forwarded-Proto/-For so it
// sees the real https scheme + client IP. The API is only reachable via the edge on the
// internal Docker network (no public binding + host firewall), so that single ingress is
// trusted. The middleware itself is only added when deployed (ForwardedHeaders:Enabled).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust forwarded headers only from the private Docker bridge range the edge sits on —
    // not from any source. The API has no public binding, so the edge is the only thing that
    // can set these. (Adjust if Docker's address pool is customised away from 172.16.0.0/12.)
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
});

var app = builder.Build();

// Must run before any middleware that reads the scheme/client IP (logging, auth, redirect).
if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();

// Correlation id first (so it's in the log context for everything), then request logging.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
    // Health probes are polled constantly; drop them to Debug so they don't drown the
    // request log. Errors/5xx still surface; everything else stays at Information.
    options.GetLevel = (httpContext, _, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Debug
                : LogEventLevel.Information);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar
}

// The Traefik edge terminates TLS and does the HTTP→HTTPS redirect; the app speaks HTTP
// in-cluster, so app-level redirect stays OFF by default (it would only log "failed to
// determine the https port"). Left as an explicit opt-in for any topology that wants the app
// to redirect — set Security:EnableHttpsRedirection=true (ForwardedHeaders is wired above).
if (app.Configuration.GetValue<bool>("Security:EnableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthEndpoints(); // /health (liveness) + /health/ready (DB + RabbitMQ)

app.Run();

namespace CompanyOps.Api
{
    /// <summary>
    /// Entry-point marker for integration tests (WebApplicationFactory&lt;ApiHost&gt;). A
    /// distinct named type avoids clashing with other referenced services' top-level Program.
    /// </summary>
    public sealed class ApiHost;
}
