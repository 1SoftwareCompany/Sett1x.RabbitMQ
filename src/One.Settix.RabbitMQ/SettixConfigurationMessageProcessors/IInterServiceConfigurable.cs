namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

public interface IInterServiceConfigurable
{
    string Contract { get; }
    string DestinationService { get; }
}
