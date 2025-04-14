using One.Settix.RabbitMQ.Bootstrap;

namespace One.Settix.RabbitMQ.Consumer;

public sealed class ConsumerPerQueueChannelResolver : ChannelResolverBase // channels per queue
{
    public ConsumerPerQueueChannelResolver(ConnectionResolver connectionResolver) : base(connectionResolver) { }
}
