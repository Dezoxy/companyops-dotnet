using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CompanyOps.Infrastructure.Messaging;

/// <summary>
/// Lazily opens and shares a single RabbitMQ connection (registered as a singleton).
/// The connection is established on first use, not at construction, so hosts that never
/// touch the broker don't connect. Channels are cheap and created per use by callers.
/// The initial connect retries with backoff so a broker that is still starting (or a
/// transient blip) doesn't take the host down; automatic recovery handles later drops.
/// </summary>
public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    : IAsyncDisposable
{
    private const int MaxConnectAttempts = 15;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        return await connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true,
            };

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    return _connection;
                }
                catch (Exception ex) when (attempt < MaxConnectAttempts && !cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "RabbitMQ not reachable (attempt {Attempt}/{Max}); retrying in {Delay}s.",
                        attempt, MaxConnectAttempts, RetryDelay.TotalSeconds);
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
