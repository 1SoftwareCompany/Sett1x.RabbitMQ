using RabbitMQ.Client;

namespace One.Settix.RabbitMQ.Bootstrap;

public interface IRabbitMqConnectionFactory
{
    IConnection CreateConnectionWithOptions(RabbitMqOptions options);
}
