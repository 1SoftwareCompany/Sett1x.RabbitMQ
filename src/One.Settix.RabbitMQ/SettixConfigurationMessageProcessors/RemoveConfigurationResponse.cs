using System.Runtime.Serialization;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name = ContractId)]
public sealed class RemoveConfigurationResponse
{
    internal const string ContractId = "b810388b-d723-4c93-8a03-ee00af788ba2";

    public RemoveConfigurationResponse(string tenant, RemoveConfigurationRequest requestPayload, bool isRestartRequired, Dictionary<string, string> data, bool isSuccess, DateTimeOffset timestamp)
    {
        Tenant = tenant;
        RequestPayload = requestPayload;
        IsRestartRequired = isRestartRequired;
        Data = data;
        IsSuccess = isSuccess;
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public RemoveConfigurationRequest RequestPayload { get; private set; }

    public bool IsRestartRequired { get; private set; }

    public Dictionary<string, string> Data { get; private set; }

    public bool IsSuccess { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }
}
