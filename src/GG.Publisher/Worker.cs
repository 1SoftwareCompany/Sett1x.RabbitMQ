using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;
using One.Settix.RabbitMQ.Publisher;

namespace GG.Publisher
{
    public class Worker : BackgroundService
    {
        private readonly SettixRabbitMqPublisher _publisher;
        private readonly SettixRabbitMqStartup _rabbitMqStartup;
        private readonly ILogger<Worker> _logger;

        public Worker(SettixRabbitMqPublisher publisher, SettixRabbitMqStartup rabbitMqStartup, ILogger<Worker> logger)
        {
            _publisher = publisher;
            _rabbitMqStartup = rabbitMqStartup;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _rabbitMqStartup.Start("giService");

            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            keyValuePairs.Add("key1", "value1");
            //_publisher.Publish(new ConfigurationRequest("tenant", "giService", keyValuePairs, DateTimeOffset.UtcNow));

            _publisher.Publish(new RemoveConfigurationRequest("tenant", "giService", keyValuePairs, true, DateTimeOffset.UtcNow));

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
