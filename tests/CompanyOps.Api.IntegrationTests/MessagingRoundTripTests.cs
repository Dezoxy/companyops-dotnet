using System.Collections.Concurrent;
using System.Net.Http.Json;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Infrastructure.Messaging;
using CompanyOps.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// End-to-end async round-trip over a real RabbitMQ: approving a request through the
/// API writes the outbox row, the API's relay publishes it, and the Worker's consumer
/// receives RequestApproved and runs the (simulated) notification.
/// </summary>
public sealed class MessagingRoundTripTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ApprovingARequest_PublishesRequestApproved_AndTheWorkerConsumesIt()
    {
        // Host the Worker's consumer against the same broker, with a capturing notifier.
        var sink = new CapturingNotificationSimulator();
        await using var connection = new RabbitMqConnection(Options.Create(factory.RabbitMqOptions));
        var consumer = new RequestApprovedConsumer(connection, sink, NullLogger<RequestApprovedConsumer>.Instance);
        await consumer.StartAsync(CancellationToken.None);

        try
        {
            var id = await FullyApproveARequestAsync();

            // The relay polls ~every 2s; wait for the event to round-trip through RabbitMQ.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            while (DateTime.UtcNow < deadline && sink.Received.All(e => e.RequestId != id))
            {
                await Task.Delay(250);
            }

            Assert.Contains(sink.Received, e => e.RequestId == id);
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    private async Task<Guid> FullyApproveARequestAsync()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        var created = await employee.PostAsJsonAsync("/requests", new { title = "Laptop", type = "Procurement" });
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<CreatedRequest>())!.Id;

        (await employee.PostAsync($"/requests/{id}/submit", content: null)).EnsureSuccessStatusCode();

        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));
        (await manager.PostAsJsonAsync($"/requests/{id}/approve", new { note = "ok" })).EnsureSuccessStatusCode();

        var finance = factory.CreateClientWithToken(await factory.GetTokenAsync("finance.user"));
        (await finance.PostAsJsonAsync($"/requests/{id}/approve", new { })).EnsureSuccessStatusCode();

        return id;
    }

    private sealed class CapturingNotificationSimulator : INotificationSimulator
    {
        public ConcurrentBag<RequestApproved> Received { get; } = [];

        public Task NotifyApprovedAsync(RequestApproved approved, CancellationToken cancellationToken)
        {
            Received.Add(approved);
            return Task.CompletedTask;
        }
    }

    private sealed record CreatedRequest(Guid Id);
}
