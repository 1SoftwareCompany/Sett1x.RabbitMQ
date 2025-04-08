namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

public interface ISettixConfigurationMessageProcessor // TODO: this needs to be thought over and changed. This interface handles 2 types of messages. This is not a real case, because you either want to consume 1 or the other...
{
    Task ProcessAsync(ConfigurationRequest message);
    Task ProcessAsync(ConfigurationResponse message);
    Task ProcessAsync(RemoveConfigurationRequest message) => Task.CompletedTask;
    Task ProcessAsync(RemoveConfigurationResponse message) => Task.CompletedTask;
}
