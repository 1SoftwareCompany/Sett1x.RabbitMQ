using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

public interface IInterServiceConfigurable
{
    string Contract { get; }
    string DestinationService { get; }
}

[DataContract(Name = ContractId)]
public sealed class ConfigureService : IInterServiceConfigurable
{
    internal const string ContractId = "dd1fe10d-694d-4bff-ba95-c86b74b32ed9";

    ConfigureService()
    {
        Data = new Dictionary<string, string>();
    }

    public ConfigureService(string tenant, string serviceKeyToConfigure, string serviceKeyToReplyBack, Dictionary<string, string> data, DateTimeOffset timestamp)
    {
        Tenant = tenant;
        ServiceKeyToConfigure = serviceKeyToConfigure;
        ServiceKeyToReplyBack = serviceKeyToReplyBack;
        Data = data ?? new Dictionary<string, string>();
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public string ServiceKeyToConfigure { get; private set; }

    public string ServiceKeyToReplyBack { get; private set; }

    public Dictionary<string, string> Data { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string Contract => ContractId;

    public string DestinationService => ServiceKeyToConfigure;
}
