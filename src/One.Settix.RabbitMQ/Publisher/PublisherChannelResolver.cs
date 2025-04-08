﻿using One.Settix.RabbitMQ.Bootstrap;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace One.Settix.RabbitMQ.Publisher;

public class PublisherChannelResolver : ChannelResolverBase // channels per exchange
{
    public PublisherChannelResolver(ConnectionResolver connectionResolver) : base(connectionResolver) { }

    public override IModel Resolve(string exchange, RabbitMqOptions options, string serviceKey)
    {
        string channelKey = $"{serviceKey}_{exchange}_{options.Server}".ToLower();
        string connectionKey = $"{options.VHost}_{options.Server}".ToLower();

        IModel channel = GetExistingChannel(channelKey);

        if (channel is null || channel.IsClosed)
        {
            lock (@lock)
            {
                channel = GetExistingChannel(channelKey);

                if (channel?.IsClosed == true)
                {
                    channels.Remove(channelKey);
                    channel = null;
                }

                if (channel is null)
                {
                    var connection = connectionResolver.Resolve(connectionKey, options);
                    IModel scopedChannel = CreateModelForPublisher(connection);
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
                        scopedChannel = CreateModelForPublisher(connection);
                        scopedChannel.ExchangeDeclare(exchange, ExchangeType.Direct, true);
                    }

                    channels.Add(channelKey, scopedChannel);
                }
            }
        }

        return GetExistingChannel(channelKey);
    }

    private IModel CreateModelForPublisher(IConnection connection)
    {
        IModel channel = connection.CreateModel();
        channel.ConfirmSelect();

        return channel;
    }
}
