using One.Settix.RabbitMQ.Bootstrap;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace One.Settix.RabbitMQ.Publisher;

public sealed class PublisherChannelResolver : ChannelResolverBase // channels per exchange
{
    private static SemaphoreSlim publisherLock = new SemaphoreSlim(1, 1); // It's crucial to set values for initial and max count of allowed threads, otherwise it is possible to allow more than expected threads to enter the lock.

    public PublisherChannelResolver(ConnectionResolver connectionResolver) : base(connectionResolver) { }

    public override async ValueTask<IModel> ResolveAsync(string exchange, RabbitMqOptions options, string serviceKey, CancellationToken cancellationToken = default)
    {
        string channelKey = $"{serviceKey}_{exchange}_{options.Server}".ToLower();
        string connectionKey = $"{options.VHost}_{options.Server}".ToLower();

        IModel channel = GetExistingChannel(channelKey);

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
                    IModel scopedChannel = CreateChannelForPublisher(connection);
                    try
                    {
                        if (string.IsNullOrEmpty(exchange) == false)
                        {
                            scopedChannel.ExchangeDeclarePassive(exchange);
                        }
                    }
                    catch (OperationInterruptedException)
                    {
                        scopedChannel.Dispose();
                        scopedChannel = CreateChannelForPublisher(connection);
                        scopedChannel.ExchangeDeclare(exchange, ExchangeType.Direct, true);
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

    private IModel CreateChannelForPublisher(IConnection connection)
    {
        IModel channel = connection.CreateModel();
        channel.ConfirmSelect();

        return channel;
    }
}
