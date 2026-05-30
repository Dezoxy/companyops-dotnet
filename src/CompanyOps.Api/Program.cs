using System.Text.Json.Serialization;
using CompanyOps.Api.Auth;
using CompanyOps.Api.ErrorHandling;
using CompanyOps.Application;
using CompanyOps.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace CompanyOps.Api
{
    /// <summary>
    /// Entry-point marker for integration tests (WebApplicationFactory&lt;ApiHost&gt;). A
    /// distinct named type avoids clashing with other referenced services' top-level Program.
    /// </summary>
    public sealed class ApiHost;
}
