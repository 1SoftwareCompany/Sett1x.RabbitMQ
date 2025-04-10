using RabbitMQ.Client;

namespace One.Settix.RabbitMQ.Bootstrap;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionWithOptionsAsync(RabbitMqOptions options);
}
