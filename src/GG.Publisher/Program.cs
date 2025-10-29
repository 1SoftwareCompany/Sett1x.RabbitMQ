using GG.Publisher;
using One.Settix.RabbitMQ;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddLogging();
builder.Services.AddSettix();
builder.Services.AddSingleton<ISettixConfigurationMessageProcessor, TestProcessor>();
builder.Services.AddHttpClient();

var host = builder.Build();
host.Run();


public class TestProcessor : ISettixConfigurationMessageProcessor
{
    private readonly ILogger<TestProcessor> logger;

    public TestProcessor(ILogger<TestProcessor> logger)
    {
        this.logger = logger;
    }

    public Task ProcessAsync(ConfigureService message)
    {
        logger.LogInformation("Hello service {service} with tenant {tenant}", message.ServiceKeyToConfigure, message.Tenant);
        return Task.CompletedTask;
    }

    public Task ProcessAsync(ServiceConfigured message)
    {
        throw new NotImplementedException();
    }

    public Task ProcessAsync(RemoveConfiguration message)
    {
        logger.LogInformation("Removed config {key} for tenant {tenant}", message.ServiceKeyToConfigure, message.Tenant);
        return Task.CompletedTask;
    }

    public Task ProcessAsync(ConfigurationRemoved message)
    {
        throw new NotImplementedException();
    }
}
