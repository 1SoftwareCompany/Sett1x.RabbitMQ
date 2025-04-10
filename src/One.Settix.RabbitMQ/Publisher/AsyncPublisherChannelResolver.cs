using One.Settix.RabbitMQ.Bootstrap;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace One.Settix.RabbitMQ.Publisher;

public class AsyncPublisherChannelResolver : AsyncChannelResolverBase // channels per exchange
{
    private static SemaphoreSlim asyncLock = new SemaphoreSlim(1);

    public AsyncPublisherChannelResolver(AsyncConnectionResolver connectionResolver) : base(connectionResolver) { }

    public override async ValueTask<IChannel> ResolveAsync(string exchange, RabbitMqOptions options, string serviceKey)
    {
        string channelKey = $"{serviceKey}_{exchange}_{options.Server}".ToLower();
        string connectionKey = $"{options.VHost}_{options.Server}".ToLower();

        IChannel channel = GetExistingChannel(channelKey);

        if (channel is null || channel.IsClosed)
        {
            await asyncLock.WaitAsync(1000).ConfigureAwait(false);

            try
            {
                channel = GetExistingChannel(channelKey);

                if (channel?.IsClosed == true)
                {
                    channels.Remove(channelKey);
                    channel = null;
                }

                if (channel is null)
                {
                    IConnection connection = await connectionResolver.ResolveAsync(connectionKey, options).ConfigureAwait(false);
                    IChannel scopedChannel = await CreateModelForPublisherAsync(connection).ConfigureAwait(false);
                    try
                    {
                        if (string.IsNullOrEmpty(exchange) == false)
                        {
                            await scopedChannel.ExchangeDeclarePassiveAsync(exchange).ConfigureAwait(false);
                        }
                    }
                    catch (OperationInterruptedException)
                    {
                        scopedChannel.Dispose();
                        scopedChannel = await CreateModelForPublisherAsync(connection).ConfigureAwait(false);
                        await scopedChannel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, true).ConfigureAwait(false);
                    }

                    channels.Add(channelKey, scopedChannel);
                }
            }
            finally
            {
                asyncLock?.Release();
            }
        }

        return GetExistingChannel(channelKey);
    }

    private async Task<IChannel> CreateModelForPublisherAsync(IConnection connection)
    {
        CreateChannelOptions channelOpts = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: false);
        IChannel channel = await connection.CreateChannelAsync(channelOpts).ConfigureAwait(false);

        return channel;
    }
}
