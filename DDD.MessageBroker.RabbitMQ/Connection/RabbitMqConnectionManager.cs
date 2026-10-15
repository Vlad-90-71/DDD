using DDD.MessageBroker.Contracts;
using DDD.MessageBroker.RabbitMQ.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DDD.MessageBroker.RabbitMQ.Connection;

public sealed class RabbitMqConnectionManager(
    IOptions<RabbitMqOptions> options)
    : IMessageBrokerRecoveryNotifier, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public event Action? ConnectionRecovered;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var settings = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,

                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);

            _connection.RecoverySucceededAsync += OnRecoverySucceededAsync;

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task OnRecoverySucceededAsync(object sender, AsyncEventArgs args)
    {
        ConnectionRecovered?.Invoke();

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();

        _lock.Dispose();
    }
}