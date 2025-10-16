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

[DataContract(Name = ContractId)]
public sealed class ConfigureServiceV2 : IInterServiceConfigurable
{
    internal const string ContractId = "4928137f-cc54-4470-99e4-0aed21cb57cf";

    ConfigureServiceV2()
    {
        Data = new HashSet<ConfigureServiceData>();
    }

    public ConfigureServiceV2(string tenant, string serviceKeyToConfigure, string serviceKeyToReplyBack, HashSet<ConfigureServiceData> data, DateTimeOffset timestamp)
    {
        Tenant = tenant;
        ServiceKeyToConfigure = serviceKeyToConfigure;
        ServiceKeyToReplyBack = serviceKeyToReplyBack;
        Data = data ?? new HashSet<ConfigureServiceData>();
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public string ServiceKeyToConfigure { get; private set; }

    public string ServiceKeyToReplyBack { get; private set; }

    public HashSet<ConfigureServiceData> Data { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string Contract => ContractId;

    public string DestinationService => ServiceKeyToConfigure;
}

public sealed class ConfigureServiceData : IEquatable<ConfigureServiceData>
{
    ConfigureServiceData() { }

    public ConfigureServiceData(string key, string value, string valueType)
    {
        Key = key;
        Value = value;
        ValueType = valueType;
    }

    public string Key { get; private set; }

    public string Value { get; private set; }

    public string ValueType { get; private set; }

    public bool Equals(ConfigureServiceData other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ConfigureServiceData);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}
