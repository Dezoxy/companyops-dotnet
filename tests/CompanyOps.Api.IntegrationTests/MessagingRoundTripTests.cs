using System.Net.Http.Json;
using CompanyOps.Application.ExternalSystems;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.FakeExternals;
using CompanyOps.Infrastructure;
using CompanyOps.Infrastructure.Messaging;
using CompanyOps.Worker;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Full async round-trip across real RabbitMQ + Postgres + the real Worker pipeline and
/// the real FakeExternals service: approving a request publishes RequestApproved (via the
/// outbox + relay), the Worker consumes it, calls the Finance system over HTTP, and
/// records a BudgetCommitted audit entry — which surfaces through GET /audit-logs.
/// </summary>
[Collection("Integration")]
public sealed class MessagingRoundTripTests(ApiFactory factory)
{
    [Fact]
    public async Task ApprovingARequest_CommitsBudgetViaTheWorker_AndAuditsIt()
    {
        // The real Finance/Inventory mock, hosted in-memory; the Worker's gateways call it.
        using var fakeExternals = new WebApplicationFactory<FakeExternalsApp>();
        await using var worker = BuildWorker(fakeExternals);
        var consumer = new IntegrationEventConsumer(
            worker.GetRequiredService<RabbitMqConnection>(),
            worker.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<IntegrationEventConsumer>.Instance);
        await consumer.StartAsync(CancellationToken.None);

        try
        {
            var id = await factory.FullyApproveRequestAsync();
            var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

            // RequestApproved → Worker commits budget via the Finance mock.
            Assert.True(await WaitForAuditAsync(auditor, id, "BudgetCommitted"), "expected a BudgetCommitted audit entry");

            // Fulfillment → RequestFulfilled → Worker reserves the asset via the Inventory mock.
            var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));
            (await itAdmin.PostAsync($"/requests/{id}/fulfill", content: null)).EnsureSuccessStatusCode();
            Assert.True(await WaitForAuditAsync(auditor, id, "AssetReserved"), "expected an AssetReserved audit entry");
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<bool> WaitForAuditAsync(HttpClient auditor, Guid requestId, string action)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var entries = (await auditor.GetFromJsonAsync<List<AuditLogResponse>>("/audit-logs"))!;
            if (entries.Any(e => e.TargetId == requestId && e.Action == action))
            {
                return true;
            }

            await Task.Delay(250);
        }

        return false;
    }

    // A worker service provider pointed at the same Postgres + RabbitMQ as the API, with
    // the external-system gateways routed to the in-memory FakeExternals.
    private ServiceProvider BuildWorker(WebApplicationFactory<FakeExternalsApp> fakeExternals)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CompanyOps"] = factory.PostgresConnectionString,
            ["RabbitMq:Host"] = factory.RabbitMqOptions.Host,
            ["RabbitMq:Port"] = factory.RabbitMqOptions.Port.ToString(),
            ["RabbitMq:Username"] = factory.RabbitMqOptions.Username,
            ["RabbitMq:Password"] = factory.RabbitMqOptions.Password,
            ["ExternalSystems:FinanceBaseUrl"] = "http://localhost",
            ["ExternalSystems:InventoryBaseUrl"] = "http://localhost",
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddInfrastructure(configuration);
        services.AddExternalSystems(configuration);
        services.AddSingleton<INotificationSimulator, NoopNotificationSimulator>();
        services.AddScoped<IntegrationEventProcessor>();

        // Route the gateway HttpClients to the in-memory FakeExternals test server.
        foreach (var name in new[] { nameof(IFinanceGateway), nameof(IInventoryGateway) })
        {
            services.Configure<HttpClientFactoryOptions>(name, options =>
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                    builder.PrimaryHandler = fakeExternals.Server.CreateHandler()));
        }

        return services.BuildServiceProvider();
    }

    private sealed class NoopNotificationSimulator : INotificationSimulator
    {
        public Task NotifyApprovedAsync(RequestApproved approved, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed record AuditLogResponse(string Action, Guid TargetId);
}
