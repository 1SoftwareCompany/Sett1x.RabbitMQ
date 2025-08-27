using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using One.Settix.RabbitMQ.Bootstrap.Management;
using One.Settix.RabbitMQ.Bootstrap.Management.Model;
using RabbitMQ.Client;

namespace One.Settix.RabbitMQ.Bootstrap;

public sealed class SettixRabbitMqConfiguration
{
    private readonly RabbitMqOptions _options;
    private readonly SettixRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<SettixRabbitMqConfiguration> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SettixRabbitMqConfiguration(IOptionsMonitor<RabbitMqOptions> optionsMonitor, SettixRabbitMqConnectionFactory connectionFactory, IHttpClientFactory httpClientFactory, ILogger<SettixRabbitMqConfiguration> logger)
    {
        _options = optionsMonitor.CurrentValue;
        _connectionFactory = connectionFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// This method is automatically invoked by <see cref="SettixRabbitMqConsumerFactory"/>
    /// </summary>
    /// <param name="queuePrefix"></param>
    /// <returns></returns>
    public async Task ConfigureAsync(string queuePrefix)
    {
        try
        {
            RabbitMqManagementClient rmqClient = new RabbitMqManagementClient(_httpClientFactory, _options);
            await CreateVHost(rmqClient).ConfigureAwait(false);

            using var connection = await _connectionFactory.CreateConnectionWithOptionsAsync(_options).ConfigureAwait(false);
            using var channel = connection.CreateModel();
            await RecoverChannelAsync(channel, queuePrefix).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Sett1x.RabbitMQ infrastructure.");
        }
    }

    private async Task RecoverChannelAsync(IModel channel, string queuePrefix)
    {
        string exchangeName = SettixRabbitMqNamer.GetExchangeName();
        string queueName = SettixRabbitMqNamer.GetQueueName(queuePrefix);
        string routingKey = SettixRabbitMqNamer.GetRoutingKey(queuePrefix);

        channel.ExchangeDeclare(exchangeName, ExchangeType.Direct, true);
        channel.QueueDeclare(queueName, true, false, false, null);
        channel.QueueBind(queueName, exchangeName, routingKey);
    }

    private async Task CreateVHost(RabbitMqManagementClient client)
    {
        IEnumerable<Vhost> vhosts = await client.GetVHostsAsync().ConfigureAwait(false);
        if (vhosts.Any(vh => vh.Name == _options.VHost) == false)
        {
            var vhost = await client.CreateVirtualHostAsync(_options.VHost).ConfigureAwait(false);
            var rmqUsers = await client.GetUsersAsync().ConfigureAwait(false);
            var rabbitMqUser = rmqUsers.SingleOrDefault(x => x.Name == _options.Username);
            var permissionInfo = new PermissionInfo(rabbitMqUser, vhost);
            await client.CreatePermissionAsync(permissionInfo);
        }
    }
}
