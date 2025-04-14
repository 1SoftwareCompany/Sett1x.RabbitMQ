using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;
using One.Settix.RabbitMQ.Publisher;
using One.Settix.RabbitMQ.Consumer;

namespace GG.Publisher
{
    public class Worker : BackgroundService
    {
        private readonly SettixPublisher _publisher;
        private readonly SettixRabbitMqConsumerFactory consumerFactory;
        private readonly ILogger<Worker> _logger;

        public Worker(SettixPublisher publisher, SettixRabbitMqConsumerFactory consumerFactory, ILogger<Worker> logger)
        {
            _publisher = publisher;
            this.consumerFactory = consumerFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await consumerFactory.CreateAndStartConsumerAsync("origin", stoppingToken).ConfigureAwait(false);

            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            keyValuePairs.Add("key1", "value1");
            //_publisher.Publish(new ConfigurationRequest("tenant", "giService", keyValuePairs, DateTimeOffset.UtcNow));

            await _publisher.PublishAsync(new RemoveConfiguration("tenant", "destination", "origin", keyValuePairs, true, DateTimeOffset.UtcNow), stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
