using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;
using RabbitMQ.Client;

namespace One.Settix.RabbitMQ.Consumer;

public sealed class SettixRabbitMqConsumerFactory
{
    private AsyncConsumer _consumer;

    private readonly ISettixConfigurationMessageProcessor _settixConfigurationMessageProcessor;
    private readonly RabbitMqOptions options;
    private readonly ConsumerPerQueueChannelResolver _channelResolver;
    private readonly ILogger<SettixRabbitMqConsumerFactory> _logger;

    public SettixRabbitMqConsumerFactory(ISettixConfigurationMessageProcessor settixConfigurationMessageProcessor, IOptionsMonitor<RabbitMqOptions> optionsMonitor, ConsumerPerQueueChannelResolver channelResolver, ILogger<SettixRabbitMqConsumerFactory> logger)
    {
        _settixConfigurationMessageProcessor = settixConfigurationMessageProcessor;
        options = optionsMonitor.CurrentValue; //TODO: Implement onChange event
        _channelResolver = channelResolver;
        _logger = logger;
    }

    public void CreateAndStartConsumer(string serviceKey, CancellationToken cancellationToken)
    {
        try
        {
            string consumerChannelKey = SettixRabbitMqNamer.GetConsumerChannelName(serviceKey);
            IModel channel = _channelResolver.Resolve(consumerChannelKey, options, options.VHost);
            string queueName = SettixRabbitMqNamer.GetQueueName(serviceKey);

            _consumer = new AsyncConsumer(queueName, _settixConfigurationMessageProcessor, channel, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Sett1x.RabbitMQ consumer");
        }
    }

    public async Task StopConsumerAsync()
    {
        if (_consumer is null)
            return;

        await _consumer.StopAsync();
    }
}
