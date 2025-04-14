using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name = ContractId)]
public sealed class ServiceConfigured : IInterServiceConfigurable
{
    internal const string ContractId = "27a7bdea-6077-4201-a410-4e57c4e9fb65";

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
