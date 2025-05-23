using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.Consumer;

namespace GG.Consumer;

public class Worker : BackgroundService
{
    private readonly SettixRabbitMqConsumerFactory _consumerFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(SettixRabbitMqConsumerFactory consumerFactory, ILogger<Worker> logger)
    {
        _consumerFactory = consumerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumerFactory.CreateAndStartConsumerAsync("destination", stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumerFactory.StopConsumerAsync();
    }
}
