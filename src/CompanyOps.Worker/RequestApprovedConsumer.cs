using System.Text;
using System.Text.Json;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CompanyOps.Worker;

/// <summary>
/// Consumes <see cref="RequestApproved"/> from RabbitMQ and runs the simulated
/// notification. Manual ack: success acks; a malformed (unprocessable) message is
/// dead-lettered immediately; a transient failure nacks with requeue and is retried,
/// bounded by the queue's delivery limit before dead-lettering (see <see cref="MessagingTopology"/>).
/// Delivery is at-least-once: the current handler is side-effect-free (it only logs), so
/// duplicates are harmless. Any real side effect added later MUST dedup on the message
/// id first (ADR 0007).
/// </summary>
public sealed class RequestApprovedConsumer(
    RabbitMqConnection connection,
    INotificationSimulator notifier,
    ILogger<RequestApprovedConsumer> logger) : BackgroundService
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

        logger.LogInformation("Listening for approvals on queue '{Queue}'.", MessagingTopology.Queue);

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
        RequestApproved approved;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            approved = JsonSerializer.Deserialize<RequestApproved>(json)
                ?? throw new JsonException("Empty RequestApproved payload.");
        }
        catch (Exception ex)
        {
            // Unprocessable input is a permanent error — dead-letter it now rather than
            // burning redeliveries (requeue: false routes it to the dead-letter exchange).
            logger.LogError(ex, "Discarding malformed message {MessageId} (dead-lettered).", ea.BasicProperties.MessageId);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            await notifier.NotifyApprovedAsync(approved, _stoppingToken);
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // Transient failure — requeue for retry, bounded by the queue's delivery limit.
            logger.LogError(ex, "Failed to handle message {MessageId}; requeuing for retry.", ea.BasicProperties.MessageId);
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
