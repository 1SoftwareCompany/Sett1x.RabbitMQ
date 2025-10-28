using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

namespace GG.Consumer
{
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
            return Task.CompletedTask;
        }

        public Task ProcessAsync(ConfigurationRemoved message)
        {
            throw new NotImplementedException();
        }
    }
}
