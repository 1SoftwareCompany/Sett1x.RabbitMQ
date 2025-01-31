using Pandora.RabbitMQ.Bootstrap;
using Pandora.RabbitMQ.PandoraConfigurationMessageProcessors;
using Pandora.RabbitMQ.Publisher;

namespace GG.Publisher
{
    public class Worker : BackgroundService
    {
        private readonly RabbitMqPublisher _publisher;
        private readonly PandoraRabbitMqStartup _rabbitMqStartup;
        private readonly ILogger<Worker> _logger;

        public Worker(RabbitMqPublisher publisher, PandoraRabbitMqStartup rabbitMqStartup, ILogger<Worker> logger)
        {
            _publisher = publisher;
            _rabbitMqStartup = rabbitMqStartup;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _rabbitMqStartup.Start("giService");
            _rabbitMqStartup.Start("topService");

            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            keyValuePairs.Add("key1", "value1");
            _publisher.Publish(new ConfigurationMessage("tenant", "giService", keyValuePairs));
            _publisher.Publish(new ConfigurationMessage("tenant", "topService", keyValuePairs));

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
