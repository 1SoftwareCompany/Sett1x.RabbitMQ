using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;
using RabbitMQ.Client;
using System.Text.Json;

namespace One.Settix.RabbitMQ.Publisher;

public sealed class SettixRabbitMqPublisher
{
    private readonly RabbitMqClusterOptions options;
    private readonly PublisherChannelResolver _channelResolver;
    private readonly ILogger<SettixRabbitMqPublisher> _logger;

    public SettixRabbitMqPublisher(IOptionsMonitor<RabbitMqClusterOptions> optionsMonitor, PublisherChannelResolver channelResolver, ILogger<SettixRabbitMqPublisher> logger)
    {
        options = optionsMonitor.CurrentValue; // TODO: Implement onChange event
        _channelResolver = channelResolver;
        _logger = logger;
    }

    public void Publish(ConfigurationRequest message)
    {
        string exchangeName = SettixRabbitMqNamer.GetExchangeName();

        try
        {
            foreach (var option in options.Clusters)
            {
                string routingKey = SettixRabbitMqNamer.GetRoutingKey(message.ServiceKey);

                IModel exchangeModel = _channelResolver.Resolve(exchangeName, option, message.ServiceKey);
                IBasicProperties props = exchangeModel.CreateBasicProperties();
                props.Persistent = true;
                props.Headers = new Dictionary<string, object>();
                props.Headers.Add("settix-message-type", ConfigurationRequest.ContractId);

                byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

                exchangeModel.BasicPublish(exchangeName, routingKey, false, props, body);

                _logger.LogInformation("Published message: {message}", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message: {message} to {exchange}", message, exchangeName);
        }
    }

    /// <summary>
    /// Publishes the response of the configured service
    /// </summary>
    /// <param name="message">The message that contains the response of the configured service</param>
    /// <param name="serviceKey">The key that will be used to construct the routing key <see cref="SettixRabbitMqNamer.GetRoutingKey"/> where the message will be published to</param>
    public void Publish(ConfigurationResponse message, string serviceKey)
    {
        string exchangeName = SettixRabbitMqNamer.GetExchangeName();

        try
        {
            foreach (var option in options.Clusters)
            {
                string routingKey = SettixRabbitMqNamer.GetRoutingKey(serviceKey);

                IModel exchangeModel = _channelResolver.Resolve(exchangeName, option, serviceKey);
                IBasicProperties props = exchangeModel.CreateBasicProperties();
                props.Persistent = true;
                props.Headers = new Dictionary<string, object>();
                props.Headers.Add("settix-message-type", ConfigurationResponse.ContractId);

                byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

                exchangeModel.BasicPublish(exchangeName, routingKey, false, props, body);

                _logger.LogInformation("Published response message: {@message}", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish response message: {@message} to {exchange}", message, exchangeName);
        }
    }
}
