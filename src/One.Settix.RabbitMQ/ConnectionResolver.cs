using One.Settix.RabbitMQ.Bootstrap;
using RabbitMQ.Client;

namespace One.Settix.RabbitMQ;

public sealed class ConnectionResolver : IAsyncDisposable
{
    private readonly Dictionary<string, IConnection> connectionsPerVHost;
    private readonly SettixRabbitMqConnectionFactory connectionFactory;

    private static SemaphoreSlim connectionResolverLock = new SemaphoreSlim(1, 1); // It's crucial to set values for initial and max count of allowed threads, otherwise it is possible to allow more than expected threads to enter the lock.

    public ConnectionResolver(SettixRabbitMqConnectionFactory connectionFactory)
    {
        connectionsPerVHost = new Dictionary<string, IConnection>();
        this.connectionFactory = connectionFactory;
    }

    public async Task<IConnection> ResolveAsync(string key, RabbitMqOptions options, CancellationToken cancellationToken = default)
    {
        IConnection connection = GetExistingConnection(key);

        if (connection is null || connection.IsOpen == false)
        {
            bool lockAcquired = false;
            try
            {
                lockAcquired = await connectionResolverLock.WaitAsync(10_000, cancellationToken).ConfigureAwait(false);
                if (lockAcquired == false)
                    throw new TimeoutException("Unable to acquire lock for connection resolver.");

                connection = GetExistingConnection(key);
                if (connection is null || connection.IsOpen == false)
                    connection = await CreateConnectionAsync(key, options, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (lockAcquired) // only release if we acquired the lock, otherwise it will throw an exception if we exceed the max count of allowed threads
                    connectionResolverLock?.Release();
            }
        }

        return connection;
    }

    /// <summary>
    /// For some reason the analyzer (as of today) reports CA1816 which does not make sense for DisposeAsync()
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        foreach (KeyValuePair<string, IConnection> connection in connectionsPerVHost)
        {
            connection.Value.Close(TimeSpan.FromSeconds(5));
        }

        connectionsPerVHost.Clear();
    }

    private IConnection GetExistingConnection(string key)
    {
        connectionsPerVHost.TryGetValue(key, out IConnection connection);

        return connection;
    }

    private async Task<IConnection> CreateConnectionAsync(string key, RabbitMqOptions options, CancellationToken cancellationToken = default)
    {
        IConnection connection = await connectionFactory.CreateConnectionWithOptionsAsync(options, cancellationToken).ConfigureAwait(false);

        if (connectionsPerVHost.TryGetValue(key, out _))
        {
            if (connectionsPerVHost.Remove(key, out _))
                connectionsPerVHost.Add(key, connection);
        }
        else
        {
            connectionsPerVHost.Add(key, connection);
        }

        return connection;
    }
}
