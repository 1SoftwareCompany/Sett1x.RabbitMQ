using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name = ContractId)]
public sealed class ServiceConfigured : IInterServiceConfigurable
{
    internal const string ContractId = "01eccbdd-d287-4601-9393-c15da6049b1c";

    ServiceConfigured()
    {
        Data = new Dictionary<string, string>();
    }

    public ServiceConfigured(string tenant, ConfigureService requestPayload, bool isRestartRequired, Dictionary<string, string> data, bool isSuccess, DateTimeOffset timestamp)
    {
        if (tenant != requestPayload.Tenant)
            throw new ArgumentException("Tenant mismatch");

        Tenant = tenant;
        RequestPayload = requestPayload;
        IsRestartRequired = isRestartRequired;
        Data = data;
        IsSuccess = isSuccess;
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public ConfigureService RequestPayload { get; private set; }

    public bool IsRestartRequired { get; private set; }

    public Dictionary<string, string> Data { get; private set; }

    public bool IsSuccess { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string Contract => ContractId;

    public string DestinationService => RequestPayload.ServiceKeyToReplyBack;
}

[DataContract(Name = ContractId)]
public sealed class ServiceConfiguredV2 : IInterServiceConfigurable
{
    internal const string ContractId = "a1940829-400f-4a70-b903-6577ec5f0e54";

    ServiceConfiguredV2()
    {
        Data = new HashSet<ConfigureServiceData>();
    }

    public ServiceConfiguredV2(string tenant, ConfigureServiceV2 requestPayload, bool isRestartRequired, HashSet<ConfigureServiceData> data, bool isSuccess, DateTimeOffset timestamp)
    {
        if (tenant != requestPayload.Tenant)
            throw new ArgumentException("Tenant mismatch");

        Tenant = tenant;
        RequestPayload = requestPayload;
        IsRestartRequired = isRestartRequired;
        Data = data;
        IsSuccess = isSuccess;
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public ConfigureServiceV2 RequestPayload { get; private set; }

    public bool IsRestartRequired { get; private set; }

    public HashSet<ConfigureServiceData> Data { get; private set; }

    public bool IsSuccess { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string Contract => ContractId;

    public string DestinationService => RequestPayload.ServiceKeyToReplyBack;
}
