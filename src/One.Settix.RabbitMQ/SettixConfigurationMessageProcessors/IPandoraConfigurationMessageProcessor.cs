namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

public interface ISettixConfigurationMessageProcessor // TODO: this needs to be thought over and changed. This interface handles 2 types of messages. This is not a real case, because you either want to consume 1 or the other...
{
    Task ProcessAsync(ConfigureService message) => Task.CompletedTask;
    Task ProcessAsync(ServiceConfigured message) => Task.CompletedTask;
    Task ProcessAsync(RemoveConfiguration message) => Task.CompletedTask;
    Task ProcessAsync(ConfigurationRemoved message) => Task.CompletedTask;

    Task ProcessAsync(ConfigureServiceV2 message);
    Task ProcessAsync(ServiceConfiguredV2 message) => Task.CompletedTask;
    Task ProcessAsync(RemoveConfigurationV2 message);
    Task ProcessAsync(ConfigurationRemovedV2 message) => Task.CompletedTask;
}
