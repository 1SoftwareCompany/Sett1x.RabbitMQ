using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name = ContractId)]
public sealed class ConfigurationRequest
{
    internal const string ContractId = "dd1fe10d-694d-4bff-ba95-c86b74b32ed9";

    public ConfigurationRequest(string tenant, string serviceKey, Dictionary<string, string> data, DateTimeOffset timestamp)
    {
        Tenant = tenant;
        ServiceKey = serviceKey;
        Data = data ?? new Dictionary<string, string>();
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public string ServiceKey { get; private set; }

    public Dictionary<string, string> Data { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }
}
