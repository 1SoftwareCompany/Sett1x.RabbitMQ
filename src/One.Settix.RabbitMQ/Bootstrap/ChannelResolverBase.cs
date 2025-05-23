﻿using RabbitMQ.Client;

namespace One.Settix.RabbitMQ.Bootstrap;

public abstract class ChannelResolverBase
{
    protected readonly Dictionary<string, IChannel> channels;
    protected readonly ConnectionResolver connectionResolver;

    protected static SemaphoreSlim channelResolverLock = new SemaphoreSlim(1, 1); // It's crucial to set values for initial and max count of allowed threads, otherwise it is possible to allow more than expected threads to enter the lock.

    public ChannelResolverBase(ConnectionResolver connectionResolver)
    {
        channels = new Dictionary<string, IChannel>();
        this.connectionResolver = connectionResolver;
    }

    public virtual async ValueTask<IChannel> ResolveAsync(string resolveKey, RabbitMqOptions options, string boundedContext, CancellationToken cancellationToken = default)
    {
        resolveKey = resolveKey.ToLower();

        IChannel channel = GetExistingChannel(resolveKey);

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
                    var connection = await connectionResolver.ResolveAsync(boundedContext, options, cancellationToken).ConfigureAwait(false);
                    CreateChannelOptions channelOpts = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: false);
                    IChannel scopedChannel = await connection.CreateChannelAsync(channelOpts, cancellationToken).ConfigureAwait(false);

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

    protected IChannel GetExistingChannel(string resolveKey)
    {
        channels.TryGetValue(resolveKey, out IChannel channel);

        return channel;
    }
}
