using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;
using RabbitMQ.Client;
using System.Text.Json;

namespace One.Settix.RabbitMQ.Publisher;

public sealed class AsyncSettixRabbitMqPublisher
{
    private readonly RabbitMqClusterOptions options;
    private readonly AsyncPublisherChannelResolver _channelResolver;
    private readonly ILogger<AsyncSettixRabbitMqPublisher> _logger;

    public AsyncSettixRabbitMqPublisher(IOptionsMonitor<RabbitMqClusterOptions> optionsMonitor, AsyncPublisherChannelResolver channelResolver, ILogger<AsyncSettixRabbitMqPublisher> logger)
    {
        options = optionsMonitor.CurrentValue; // TODO: Implement onChange event
        _channelResolver = channelResolver;
        _logger = logger;
    }

    /// <summary>
    /// Publishes configuration message
    /// </summary>
    /// <param name="message">The message that contains the response of the configured service</param>
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IInterServiceConfigurable
    {
        string exchangeName = SettixRabbitMqNamer.GetExchangeName();

        try
        {
            byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);
            List<Task> publishTasks = new List<Task>();

            foreach (RabbitMqOptions option in options.Clusters)
            {
                Task publishTask = PublishToServiceAsync(message.DestinationService, option, message.Contract, body, cancellationToken);
                publishTasks.Add(publishTask);
            }

            await Task.WhenAll(publishTasks).ConfigureAwait(false);

            _logger.LogInformation("Published message: {@message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message: {@message} to {exchange}", message, exchangeName);
        }
    }

    private async Task PublishToServiceAsync(string serviceKey, RabbitMqOptions option, string bodyContract, byte[] body, CancellationToken cancellationToken)
    {
        string exchangeName = SettixRabbitMqNamer.GetExchangeName();
        string routingKey = SettixRabbitMqNamer.GetRoutingKey(serviceKey);

        IChannel exchangeChannel = await _channelResolver.ResolveAsync(exchangeName, option, serviceKey, cancellationToken).ConfigureAwait(false);
        BasicProperties props = BuildProps(bodyContract);

        await exchangeChannel.BasicPublishAsync(exchangeName, routingKey, false, props, body, cancellationToken).ConfigureAwait(false);
    }

    private static BasicProperties BuildProps(string contractId)
    {
        BasicProperties props = new BasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object>
        {
            { "settix-message-type", contractId }
        };

        return props;
    }
}
