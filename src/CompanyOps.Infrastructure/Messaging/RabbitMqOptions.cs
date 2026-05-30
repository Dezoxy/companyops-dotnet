namespace CompanyOps.Infrastructure.Messaging;

/// <summary>Connection settings for RabbitMQ, bound from the "RabbitMq" config section.</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "companyops";
    public string Password { get; init; } = "";
}
