using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name = ContractId)]
public sealed class RemoveConfiguration : IInterServiceConfigurable
{
    internal const string ContractId = "985c9cfd-9a73-4772-b007-365444315588";

    RemoveConfiguration()
    {
        Data = new Dictionary<string, string>();
    }

    public RemoveConfiguration(string tenant, string serviceKeyToConfigure, string serviceKeyToReplyBack, Dictionary<string, string> data, bool shouldWipeData, DateTimeOffset timestamp)
    {
        Tenant = tenant;
        ServiceKeyToConfigure = serviceKeyToConfigure;
        ServiceKeyToReplyBack = serviceKeyToReplyBack;
        Data = data ?? new Dictionary<string, string>();
        ShouldWipeData = shouldWipeData;
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public string ServiceKeyToConfigure { get; private set; }

    public string ServiceKeyToReplyBack { get; private set; }

    public Dictionary<string, string> Data { get; private set; }

    public bool ShouldWipeData { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string Contract => ContractId;

    public string DestinationService => ServiceKeyToConfigure;
}
