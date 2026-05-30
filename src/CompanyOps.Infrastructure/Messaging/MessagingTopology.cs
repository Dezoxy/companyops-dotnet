using RabbitMQ.Client;

namespace CompanyOps.Infrastructure.Messaging;

/// <summary>
/// The RabbitMQ topology shared by the relay (publisher) and the Worker (consumer):
/// a durable direct exchange routed by event-type name to a single durable quorum
/// queue, with a dead-letter exchange/queue for poison messages. Declaring is
/// idempotent, so both sides may declare it on startup.
/// </summary>
public static class MessagingTopology
{
    public const string Exchange = "companyops.events";
    public const string Queue = "companyops.worker";
    public const string DeadLetterExchange = "companyops.events.dlx";
    public const string DeadLetterQueue = "companyops.worker.dead-letter";

    /// <summary>Routing key for an event — its type name (e.g. "RequestApproved").</summary>
    public static string RoutingKeyFor(string eventType) => eventType;

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, routingKey: string.Empty, cancellationToken: cancellationToken);

        // Quorum queue with a delivery limit: after repeated nack/requeue redeliveries,
        // RabbitMQ dead-letters the poison message instead of looping forever.
        var args = new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-delivery-limit"] = 5,
        };
        await channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(Queue, Exchange, RoutingKeyFor("RequestApproved"), cancellationToken: cancellationToken);
        await channel.QueueBindAsync(Queue, Exchange, RoutingKeyFor("RequestFulfilled"), cancellationToken: cancellationToken);
    }
}
