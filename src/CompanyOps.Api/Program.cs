using System.Text.Json.Serialization;
using CompanyOps.Api.Auth;
using CompanyOps.Api.ErrorHandling;
using CompanyOps.Api.Observability;
using CompanyOps.Application;
using CompanyOps.Infrastructure;
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

var app = builder.Build();

// One-shot migrator mode (used by the compose `migrator` service): apply migrations
// and exit, so the app processes never self-migrate and startup ordering is explicit.
if (args.Contains("--migrate"))
{
    await app.Services.MigrateDatabaseAsync();
    return;
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

// TLS terminates at the edge (ingress/reverse proxy); the app speaks HTTP in-cluster, so
// app-level redirect is OFF by default — it never fires in dev/compose (no HTTPS port, which
// would only log "failed to determine the https port") and never auto-arms a redirect loop on
// a deployment that lacks ForwardedHeaders. Phase 11 wires ForwardedHeaders (KnownProxies),
// then opts in explicitly via Security:EnableHttpsRedirection=true.
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
