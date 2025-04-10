using One.Settix.RabbitMQ.Bootstrap;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace One.Settix.RabbitMQ.Publisher;

public class AsyncConnectionResolver : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, IConnection> connectionsPerVHost;
    private readonly IAsyncRabbitMqConnectionFactory connectionFactory;
    private static SemaphoreSlim @lock = new SemaphoreSlim(1);

    public AsyncConnectionResolver(IAsyncRabbitMqConnectionFactory connectionFactory)
    {
        connectionsPerVHost = new ConcurrentDictionary<string, IConnection>();
        this.connectionFactory = connectionFactory;
    }

    public async Task<IConnection> ResolveAsync(string key, RabbitMqOptions options)
    {
        IConnection connection = GetExistingConnection(key);

        if (connection is null || connection.IsOpen == false)
        {
            await @lock.WaitAsync(1000).ConfigureAwait(false);

            try
            {
                connection = GetExistingConnection(key);
                if (connection is null || connection.IsOpen == false)
                {
                    connection = await CreateConnectionAsync(key, options).ConfigureAwait(false);
                }
            }
            finally
            {
                @lock?.Release();
            }
        }

        return connection;
    }

    private IConnection GetExistingConnection(string key)
    {
        connectionsPerVHost.TryGetValue(key, out IConnection connection);

        return connection;
    }

    private async Task<IConnection> CreateConnectionAsync(string key, RabbitMqOptions options)
    {
        IConnection connection = await connectionFactory.CreateConnectionWithOptionsAsync(options).ConfigureAwait(false);

        if (connectionsPerVHost.TryGetValue(key, out _))
        {
            if (connectionsPerVHost.TryRemove(key, out _))
                connectionsPerVHost.TryAdd(key, connection);
        }
        else
        {
            connectionsPerVHost.TryAdd(key, connection);
        }

        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in connectionsPerVHost)
        {
            await connection.Value.CloseAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }
}
