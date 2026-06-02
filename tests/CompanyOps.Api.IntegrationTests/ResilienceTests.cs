using System.Net.Http.Json;
using System.Text.Json;
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
using RabbitMQ.Client;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// The resilience paths the system is built on (ADR 0007/0008): the worker is idempotent
/// under at-least-once delivery, and a failing external system does not produce a phantom
/// success. Each test publishes a synthetic RequestApproved straight to RabbitMQ (so it
/// controls the message id) and reads the outcome back through the audit log.
/// </summary>
[Collection("Integration")]
public sealed class ResilienceTests(ApiFactory factory)
{
    [Fact]
    public async Task DuplicateDelivery_CommitsBudgetOnce()
    {
        using var fakeExternals = NewFakeExternals(failFinance: false);
        await using var worker = BuildWorker(fakeExternals);
        var consumer = StartConsumer(worker);

        try
        {
            var requestId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var evt = new RequestApproved(requestId, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

            // Same message id delivered twice — the dedup guard must let it through once.
            await PublishAsync(worker, messageId, evt);
            await PublishAsync(worker, messageId, evt);

            await WaitUntilAsync(async () => await CountBudgetCommittedAsync(requestId) >= 1);
            await Task.Delay(TimeSpan.FromSeconds(2)); // give the duplicate time to be skipped

            Assert.Equal(1, await CountBudgetCommittedAsync(requestId));
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task FailingFinanceSystem_DoesNotCommitBudget()
    {
        using var fakeExternals = NewFakeExternals(failFinance: true);
        await using var worker = BuildWorker(fakeExternals);
        var consumer = StartConsumer(worker);

        try
        {
            var requestId = Guid.NewGuid();
            var evt = new RequestApproved(requestId, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
            await PublishAsync(worker, Guid.NewGuid(), evt);

            // The Finance system 503s on every attempt, so the message is retried then
            // dead-lettered — never producing a BudgetCommitted audit.
            await Task.Delay(TimeSpan.FromSeconds(8));

            Assert.Equal(0, await CountBudgetCommittedAsync(requestId));
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    private static WebApplicationFactory<FakeExternalsApp> NewFakeExternals(bool failFinance) =>
        new WebApplicationFactory<FakeExternalsApp>().WithWebHostBuilder(builder =>
            builder.UseSetting("FakeExternals:FailFinance", failFinance ? "true" : "false"));

    private IntegrationEventConsumer StartConsumer(ServiceProvider worker)
    {
        var consumer = new IntegrationEventConsumer(
            worker.GetRequiredService<RabbitMqConnection>(),
            worker.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<IntegrationEventConsumer>.Instance);
        consumer.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        return consumer;
    }

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

        foreach (var name in new[] { nameof(IFinanceGateway), nameof(IInventoryGateway) })
        {
            services.Configure<HttpClientFactoryOptions>(name, options =>
                options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = fakeExternals.Server.CreateHandler()));
        }

        return services.BuildServiceProvider();
    }

    private static async Task PublishAsync(ServiceProvider worker, Guid messageId, RequestApproved evt)
    {
        var connection = worker.GetRequiredService<RabbitMqConnection>();
        await using var channel = await connection.CreateChannelAsync();
        await MessagingTopology.DeclareAsync(channel);

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = messageId.ToString(),
            Type = nameof(RequestApproved),
            ContentType = "application/json",
        };

        await channel.BasicPublishAsync(
            MessagingTopology.Exchange,
            MessagingTopology.RoutingKeyFor(nameof(RequestApproved)),
            mandatory: false,
            basicProperties: properties,
            body: JsonSerializer.SerializeToUtf8Bytes(evt));
    }

    private async Task<int> CountBudgetCommittedAsync(Guid requestId)
    {
        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));
        var page = await auditor.GetFromJsonAsync<PagedResponse<AuditEntry>>("/audit-logs");
        return page!.Items.Count(e => e.TargetId == requestId && e.Action == "BudgetCommitted");
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(250);
        }
    }

    private sealed class NoopNotificationSimulator : INotificationSimulator
    {
        public Task NotifyApprovedAsync(RequestApproved approved, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed record AuditEntry(string Action, Guid TargetId);

    private sealed record PagedResponse<T>(List<T> Items, int Total, int Page, int PageSize);
}
