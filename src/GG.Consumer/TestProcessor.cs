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

        public Task ProcessAsync(ConfigurationRequest message)
        {
            logger.LogInformation("Hello service {service} with tenant {tenant}", message.ServiceKey, message.Tenant);
            return Task.CompletedTask;
        }

        public Task ProcessAsync(ConfigurationResponse message)
        {
            throw new NotImplementedException();
        }

        public Task ProcessAsync(RemoveConfigurationRequest message)
        {
            return Task.CompletedTask;
        }
    }
}
