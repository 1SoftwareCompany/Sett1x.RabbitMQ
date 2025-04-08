using One.Settix.RabbitMQ.Bootstrap;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace One.Settix.RabbitMQ.Publisher;

public class ConnectionResolver : IDisposable
{
    private readonly ConcurrentDictionary<string, IConnection> connectionsPerVHost;
    private readonly IRabbitMqConnectionFactory connectionFactory;
    private static readonly object connectionLock = new object(); // TODO: Use lock object after update to .NET 9

    public ConnectionResolver(IRabbitMqConnectionFactory connectionFactory)
    {
        connectionsPerVHost = new ConcurrentDictionary<string, IConnection>();
        this.connectionFactory = connectionFactory;
    }

    public IConnection Resolve(string key, RabbitMqOptions options)
    {
        IConnection connection = GetExistingConnection(key);

        if (connection is null || connection.IsOpen == false)
        {
            lock (connectionLock)
            {
                connection = GetExistingConnection(key);
                if (connection is null || connection.IsOpen == false)
                {
                    connection = CreateConnection(key, options);
                }
            }
        }

        return connection;
    }

    private IConnection GetExistingConnection(string key)
    {
        connectionsPerVHost.TryGetValue(key, out IConnection connection);

        return connection;
    }

    private IConnection CreateConnection(string key, RabbitMqOptions options)
    {
        IConnection connection = connectionFactory.CreateConnectionWithOptions(options);

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

    public void Dispose()
    {
        foreach (var connection in connectionsPerVHost)
        {
            connection.Value.Close(TimeSpan.FromSeconds(5));
        }
    }
}
