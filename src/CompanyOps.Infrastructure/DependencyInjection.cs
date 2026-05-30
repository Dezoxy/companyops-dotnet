using CompanyOps.Application.Abstractions;
using CompanyOps.Application.ExternalSystems;
using CompanyOps.Infrastructure.ExternalSystems;
using CompanyOps.Infrastructure.Messaging;
using CompanyOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        services.AddMessaging(configuration);

        return services;
    }

    /// <summary>
    /// Registers the shared RabbitMQ connection (lazy — connects on first use). Used by
    /// the API (via <see cref="AddInfrastructure"/>) and standalone by the Worker, which
    /// needs the broker but not the database.
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqConnection>();
        return services;
    }

    /// <summary>
    /// Registers the external-system gateways as resilient typed HttpClients (timeout +
    /// retry via the standard resilience handler). Used by the Worker (ADR 0008).
    /// </summary>
    public static IServiceCollection AddExternalSystems(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ExternalSystemsOptions.SectionName).Get<ExternalSystemsOptions>()
            ?? throw new InvalidOperationException("ExternalSystems configuration is missing.");

        services.AddHttpClient<IFinanceGateway, FinanceGateway>(client =>
            client.BaseAddress = new Uri(options.FinanceBaseUrl)).AddStandardResilienceHandler();

        services.AddHttpClient<IInventoryGateway, InventoryGateway>(client =>
            client.BaseAddress = new Uri(options.InventoryBaseUrl)).AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// Registers the outbox relay (the producer-side publisher). Call from the API host
    /// only — never the Worker, or the outbox would be published twice.
    /// </summary>
    public static IServiceCollection AddOutboxRelay(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqPublisher>();
        services.AddHostedService<OutboxRelay>();
        return services;
    }
}
