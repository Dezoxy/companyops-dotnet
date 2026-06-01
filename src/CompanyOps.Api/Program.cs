using System.Reflection;
using System.Text.Json.Serialization;
using CompanyOps.Api.Auth;
using CompanyOps.Api.Cors;
using CompanyOps.Api.ErrorHandling;
using CompanyOps.Api.Observability;
using CompanyOps.Api.RateLimiting;
using CompanyOps.Application;
using CompanyOps.Application.Abstractions;
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

// Build-time OpenAPI generation (Microsoft.Extensions.ApiDescription.Server) loads this app to
// emit the document: it runs the full Program up to — but not including — app.Run(), with no DB /
// broker / Keycloak config available. The lazy registrations (EF, JWT, RabbitMQ) never connect at
// build time, so harmless placeholder config lets the host construct without weakening the real
// fail-fast: a genuine boot still reads (and requires) the real values.
if (Assembly.GetEntryAssembly()?.GetName().Name is "dotnet-getdocument" or "GetDocument.Insider")
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:CompanyOps"] = "Host=openapi-build;Database=openapi;Username=openapi;Password=openapi",
        ["Keycloak:Authority"] = "https://openapi-build.invalid/realms/companyops",
        ["Keycloak:Audience"] = "companyops-api",
        ["RabbitMq:Host"] = "openapi-build.invalid",
        ["RabbitMq:Username"] = "openapi",
        ["RabbitMq:Password"] = "openapi",
    });
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
// A valid-but-insufficient principal (missing sub/department) → 403, not a leaked 500.
builder.Services.AddExceptionHandler<MissingClaimExceptionHandler>();
// A clash with existing state (e.g. a duplicate asset tag) → 409, not a leaked 500.
builder.Services.AddExceptionHandler<ConflictExceptionHandler>();

// TimeProvider.System makes "now" injectable and testable (no custom clock port).
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddCompanyOpsAuthorization();
builder.Services.AddCompanyOpsCors(builder.Configuration);
builder.Services.AddCompanyOpsRateLimiting(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// API-originated audits record the caller's IP — override the Infrastructure default.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditContext, HttpAuditContext>();
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

// CORS before auth so the SPA's preflight (OPTIONS) isn't rejected by the 401 challenge.
app.UseCors(CorsSetup.PolicyName);

app.UseAuthentication();
// After authentication (so the limit partitions on the user) but before authorization/endpoints.
app.UseRateLimiter();
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
