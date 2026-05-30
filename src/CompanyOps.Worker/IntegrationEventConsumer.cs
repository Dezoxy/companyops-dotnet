using System.Text;
using CompanyOps.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CompanyOps.Worker;

/// <summary>
/// Consumes integration events (RequestApproved / RequestFulfilled) and dispatches each
/// to <see cref="IntegrationEventProcessor"/> in its own DI scope. Manual ack: success
/// acks; a malformed message dead-letters immediately; a transient failure (e.g. the
/// external system being down) requeues, bounded by the queue's delivery limit.
/// </summary>
public sealed class IntegrationEventConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<IntegrationEventConsumer> logger) : BackgroundService
{
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        _channel = await connection.CreateChannelAsync(stoppingToken);
        await MessagingTopology.DeclareAsync(_channel, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;
        await _channel.BasicConsumeAsync(MessagingTopology.Queue, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Listening for integration events on queue '{Queue}'.", MessagingTopology.Queue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var messageId = Guid.TryParse(ea.BasicProperties.MessageId, out var id) ? id : Guid.Empty;
        var type = ea.BasicProperties.Type ?? string.Empty;

        try
        {
            // No usable message id ⇒ no dedup key, so we can't safely process it
            // idempotently. Treat as malformed and dead-letter rather than risk a
            // duplicate side effect or poisoning the Guid.Empty dedup slot.
            if (messageId == Guid.Empty)
            {
                throw new MalformedMessageException("Message is missing a usable message id.");
            }

            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IntegrationEventProcessor>();
            var json = Encoding.UTF8.GetString(ea.Body.Span);

            await processor.ProcessAsync(messageId, type, json, _stoppingToken);
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (MalformedMessageException ex)
        {
            logger.LogError(ex, "Discarding malformed message {MessageId} (dead-lettered).", messageId);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle message {MessageId}; requeuing for retry.", messageId);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
    }
}
