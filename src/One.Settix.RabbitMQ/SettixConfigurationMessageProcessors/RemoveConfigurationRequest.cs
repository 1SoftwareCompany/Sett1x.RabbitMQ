using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name = ContractId)]
public sealed class RemoveConfigurationRequest
{
    internal const string ContractId = "5691b049-add8-4fee-82b5-d4df05097122";

    public RemoveConfigurationRequest(string tenant, string serviceKey, Dictionary<string, string> data, bool shouldWipeData, DateTimeOffset timestamp)
    {
        Tenant = tenant;
        ServiceKey = serviceKey;
        Data = data ?? new Dictionary<string, string>();
        ShouldWipeData = shouldWipeData;
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public string ServiceKey { get; private set; }

    public Dictionary<string, string> Data { get; private set; }

    public bool ShouldWipeData { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }
}
