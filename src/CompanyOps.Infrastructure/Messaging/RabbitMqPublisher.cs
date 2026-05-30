using System.Text;
using RabbitMQ.Client;

namespace CompanyOps.Infrastructure.Messaging;

/// <summary>Publishes an outbox message to the broker. Used by the relay only.</summary>
internal sealed class RabbitMqPublisher(RabbitMqConnection connection)
{
    public async Task PublishAsync(Guid messageId, string type, string payloadJson, CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken);
        await MessagingTopology.DeclareAsync(channel, cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = messageId.ToString(),
            Type = type,
            ContentType = "application/json",
        };

        await channel.BasicPublishAsync(
            exchange: MessagingTopology.Exchange,
            routingKey: MessagingTopology.RoutingKeyFor(type),
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payloadJson),
            cancellationToken: cancellationToken);
    }
}
