namespace One.Settix.RabbitMQ.Bootstrap;

public sealed class RabbitMqClusterOptions
{
    public List<RabbitMqOptions> Clusters { get; set; } = new List<RabbitMqOptions>();
}

public sealed class RabbitMqOptions
{
    const string ServerDefault = "127.0.0.1";
    const int PortDefault = 5672;
    const string VHostDefault = "settix-configurations";
    const string UsernameDefault = "guest";
    const string PasswordDefault = "guest";
    const int AdminPortDefault = 5672;

    public string Server { get; set; } = ServerDefault;

    public int Port { get; set; } = PortDefault;

    public string VHost { get; set; } = VHostDefault;

    public string Username { get; set; } = UsernameDefault;

    public string Password { get; set; } = PasswordDefault;

    public int AdminPort { get; set; } = AdminPortDefault;

    public string ApiAddress { get; set; }

    public bool UseSsl { get; set; } = false;
}
