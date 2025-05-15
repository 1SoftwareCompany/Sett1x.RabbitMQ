using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name = ContractId)]
public sealed class ConfigureService : IInterServiceConfigurable
{
    internal const string ContractId = "98a96efe-9bb8-42ef-a379-26871af71a6b";

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
