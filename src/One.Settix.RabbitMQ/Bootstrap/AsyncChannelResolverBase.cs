using One.Settix.RabbitMQ.Publisher;
using RabbitMQ.Client;

namespace One.Settix.RabbitMQ.Bootstrap;

public abstract class AsyncChannelResolverBase
{
    protected readonly Dictionary<string, IChannel> channels;
    protected readonly AsyncConnectionResolver connectionResolver;
    protected static SemaphoreSlim @lock = new SemaphoreSlim(1);

    public AsyncChannelResolverBase(AsyncConnectionResolver connectionResolver)
    {
        channels = new Dictionary<string, IChannel>();
        this.connectionResolver = connectionResolver;
    }

    public virtual async ValueTask<IChannel> ResolveAsync(string resolveKey, RabbitMqOptions options, string boundedContext)
    {
        resolveKey = resolveKey.ToLower();

        IChannel channel = GetExistingChannel(resolveKey);

        if (channel is null || channel.IsClosed)
        {
            try
            {
                await @lock.WaitAsync(1000).ConfigureAwait(false);

                {
                    channel = GetExistingChannel(resolveKey);

                    if (channel?.IsClosed == true)
                    {
                        channels.Remove(resolveKey);
                        channel = null;
                    }

                    if (channel is null)
                    {
                        var connection = await connectionResolver.ResolveAsync(boundedContext, options).ConfigureAwait(false);
                        CreateChannelOptions channelOpts = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: false);
                        IChannel scopedChannel = await connection.CreateChannelAsync(channelOpts).ConfigureAwait(false);

                        channels.Add(resolveKey, scopedChannel);
                    }
                }
            }
            finally
            {
                @lock?.Release();
            }
        }

        return GetExistingChannel(resolveKey);
    }

    protected IChannel GetExistingChannel(string resolveKey)
    {
        channels.TryGetValue(resolveKey, out IChannel channel);

        return channel;
    }
}
