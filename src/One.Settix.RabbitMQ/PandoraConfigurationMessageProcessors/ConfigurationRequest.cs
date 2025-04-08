using System.Runtime.Serialization;
using System.Text;

namespace One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

[DataContract(Name ="dd1fe10d-694d-4bff-ba95-c86b74b32ed9")]
public sealed class ConfigurationRequest
{
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

    public static string ContractId = GetContractIdFromAttribute(typeof(ConfigurationRequest));

    private static string GetContractIdFromAttribute(Type contractType)
    {
        string contractId;
        DataContractAttribute contract = contractType
            .GetCustomAttributes(false).Where(attr => attr is DataContractAttribute)
            .SingleOrDefault() as DataContractAttribute;

        if (contract == null || string.IsNullOrEmpty(contract.Name))
        {
            throw new Exception(string.Format(@"The message type '{0}' is missing a DataContract attribute. Example: [DataContract(""00000000-0000-0000-0000-000000000000"")]", contractType.FullName));
        }
        else
        {
            contractId = contract.Name;
        }

        return contractId;
    }
}

[DataContract(Name = "27a7bdea-6077-4201-a410-4e57c4e9fb65")]
public sealed class ConfigurationResponse
{
    public ConfigurationResponse(string tenant, ConfigurationRequest requestPayload, bool isRestartRequired, Dictionary<string, string> data, bool isSuccess, DateTimeOffset timestamp)
    {
        Tenant = tenant;
        RequestPayload = requestPayload;
        IsRestartRequired = isRestartRequired;
        Data = data;
        IsSuccess = isSuccess;
        Timestamp = timestamp;
    }

    public string Tenant { get; private set; }

    public ConfigurationRequest RequestPayload { get; private set; }

    public bool IsRestartRequired { get; private set; }

    public Dictionary<string, string> Data { get; private set; }

    public bool IsSuccess { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public static string ContractId = GetContractIdFromAttribute(typeof(ConfigurationResponse));

    private static string GetContractIdFromAttribute(Type contractType)
    {
        string contractId;
        DataContractAttribute contract = contractType
            .GetCustomAttributes(false).Where(attr => attr is DataContractAttribute)
            .SingleOrDefault() as DataContractAttribute;

        if (contract == null || string.IsNullOrEmpty(contract.Name))
        {
            throw new Exception(string.Format(@"The message type '{0}' is missing a DataContract attribute. Example: [DataContract(""00000000-0000-0000-0000-000000000000"")]", contractType.FullName));
        }
        else
        {
            contractId = contract.Name;
        }

        return contractId;
    }
}
