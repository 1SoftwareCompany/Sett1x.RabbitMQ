using RabbitMQ.Client;

namespace One.Settix.RabbitMQ.Bootstrap;

public abstract class ChannelResolverBase
{
    protected readonly Dictionary<string, IModel> channels;
    protected readonly ConnectionResolver connectionResolver;

    protected static SemaphoreSlim channelResolverLock = new SemaphoreSlim(1, 1); // It's crucial to set values for initial and max count of allowed threads, otherwise it is possible to allow more than expected threads to enter the lock.

    public ChannelResolverBase(ConnectionResolver connectionResolver)
    {
        channels = new Dictionary<string, IModel>();
        this.connectionResolver = connectionResolver;
    }

    public virtual async ValueTask<IModel> ResolveAsync(string resolveKey, RabbitMqOptions options, string boundedContext, CancellationToken cancellationToken = default)
    {
        resolveKey = resolveKey.ToLower();

        IModel channel = GetExistingChannel(resolveKey);

        if (channel is null || channel.IsClosed)
        {
            bool lockAcquired = false;
            try
            {
                lockAcquired = await channelResolverLock.WaitAsync(10_000, cancellationToken).ConfigureAwait(false);
                if (lockAcquired == false)
                    throw new TimeoutException("Unable to acquire lock for channel resolver.");

                channel = GetExistingChannel(resolveKey);

                if (channel?.IsClosed == true)
                {
                    channels.Remove(resolveKey);
                    channel = null;
                }

                if (channel is null)
                {
                    IConnection connection = await connectionResolver.ResolveAsync(boundedContext, options, cancellationToken).ConfigureAwait(false);
                    IModel scopedChannel = connection.CreateModel();
                    scopedChannel.ConfirmSelect();

                    channels.Add(resolveKey, scopedChannel);
                }
            }
            finally
            {
                if (lockAcquired) // only release if we acquired the lock, otherwise it will throw an exception if we exceed the max count of allowed threads
                    channelResolverLock?.Release();
            }
        }

        return GetExistingChannel(resolveKey);
    }

    protected IModel GetExistingChannel(string resolveKey)
    {
        channels.TryGetValue(resolveKey, out IModel channel);

        return channel;
    }
}
