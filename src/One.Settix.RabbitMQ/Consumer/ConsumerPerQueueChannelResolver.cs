using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.Publisher;

namespace One.Settix.RabbitMQ.Consumer;

public class ConsumerPerQueueChannelResolver : ChannelResolverBase // channels per queue
{
    public ConsumerPerQueueChannelResolver(AsyncConnectionResolver connectionResolver) : base(connectionResolver) { }
}
