using CompanyOps.Application.Abstractions;
using CompanyOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CompanyOps")
            ?? throw new InvalidOperationException(
                "Connection string 'CompanyOps' is not configured. Set ConnectionStrings__CompanyOps via environment.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Expose the context as the unit of work and register repositories.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IRequestRepository, RequestRepository>();

        // One AuditLogStore per scope, exposed as both the write and read ports.
        services.AddScoped<AuditLogStore>();
        services.AddScoped<IAuditLogger>(sp => sp.GetRequiredService<AuditLogStore>());
        services.AddScoped<IAuditLogReader>(sp => sp.GetRequiredService<AuditLogStore>());

        // Integration events are written to the outbox in the same transaction (ADR 0007).
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

        return services;
    }
}
