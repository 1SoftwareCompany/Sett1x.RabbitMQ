using One.Settix.RabbitMQ.Bootstrap;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace One.Settix.RabbitMQ.Publisher;

public sealed class PublisherChannelResolver : ChannelResolverBase // channels per exchange
{
    private static SemaphoreSlim publisherLock = new SemaphoreSlim(1, 1); // It's crucial to set values for initial and max count of allowed threads, otherwise it is possible to allow more than expected threads to enter the lock.

    public PublisherChannelResolver(ConnectionResolver connectionResolver) : base(connectionResolver) { }

    public override async ValueTask<IChannel> ResolveAsync(string exchange, RabbitMqOptions options, string serviceKey, CancellationToken cancellationToken = default)
    {
        string channelKey = $"{serviceKey}_{exchange}_{options.Server}".ToLower();
        string connectionKey = $"{options.VHost}_{options.Server}".ToLower();

        IChannel channel = GetExistingChannel(channelKey);

        if (channel is null || channel.IsClosed)
        {
            bool lockAcquired = false;
            try
            {
                lockAcquired = await publisherLock.WaitAsync(10_000, cancellationToken).ConfigureAwait(false);
                if (lockAcquired == false)
                {
                    throw new TimeoutException("Unable to acquire lock for publisher channel resolver.");
                }

                channel = GetExistingChannel(channelKey);

                if (channel?.IsClosed == true)
                {
                    channels.Remove(channelKey);
                    channel = null;
                }

                if (channel is null)
                {
                    IConnection connection = await connectionResolver.ResolveAsync(connectionKey, options, cancellationToken).ConfigureAwait(false);
                    IChannel scopedChannel = await CreateChannelForPublisherAsync(connection, cancellationToken).ConfigureAwait(false);
                    try
                    {
                        if (string.IsNullOrEmpty(exchange) == false)
                        {
                            await scopedChannel.ExchangeDeclarePassiveAsync(exchange, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationInterruptedException)
                    {
                        scopedChannel.Dispose();
                        scopedChannel = await CreateChannelForPublisherAsync(connection).ConfigureAwait(false);
                        await scopedChannel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, true).ConfigureAwait(false);
                    }

                    channels.Add(channelKey, scopedChannel);
                }
            }
            finally
            {
                if (lockAcquired) // only release if we acquired the lock, otherwise it will throw an exception if we exceed the max count of allowed threads
                    publisherLock?.Release();
            }
        }

        return GetExistingChannel(channelKey);
    }

    private async Task<IChannel> CreateChannelForPublisherAsync(IConnection connection, CancellationToken cancellationToken = default)
    {
        CreateChannelOptions channelOpts = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: false);
        IChannel channel = await connection.CreateChannelAsync(channelOpts, cancellationToken).ConfigureAwait(false);

        return channel;
    }
}
