using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace One.Settix.RabbitMQ.Bootstrap;

public sealed class SettixRabbitMqNamer
{
    static Regex regex = new Regex(@"^\b([\w\d_-]+$)");
    private const string SettixConfigurationsExchange = "settix.configurations";

    public static string GetQueueName(string queuePrefix)
    {
        string lowQueuePrefix = queuePrefix.ToLower();
        if (regex.Match(lowQueuePrefix).Success == false)
            throw new ValidationException($"The queue name '{lowQueuePrefix}' is not valid. It should contain only letters, digits, and underscores.");

        return $"{lowQueuePrefix}.config";
    }

    public static string GetRoutingKey(string routingKeyPrefix)
    {
        string lowRoutingKeyPrefix = routingKeyPrefix.ToLower();
        if (regex.Match(lowRoutingKeyPrefix).Success == false)
            throw new ValidationException($"The routing key '{lowRoutingKeyPrefix}' is not valid. It should contain only letters, digits, and underscores.");

        return $"{lowRoutingKeyPrefix}.config.routingkey";
    }

    public static string GetExchangeName()
    {
        return SettixConfigurationsExchange;
    }

    public static string GetConsumerChannelName(string channelPrefix)
    {
        string lowChannelPrefix = channelPrefix.ToLower();
        if (regex.Match(lowChannelPrefix).Success == false)
            throw new ValidationException($"The channel name '{lowChannelPrefix}' is not valid. It should contain only letters, digits, and underscores.");

        return $"{lowChannelPrefix}.consumer";
    }

    public static string GetPublisherChannelName(string channelPrefix, string exchange, string server)
    {
        if (regex.Match(channelPrefix).Success == false)
            throw new ValidationException($"The channel name '{channelPrefix}' is not valid. It should contain only letters, digits, and underscores.");

        return $"{channelPrefix}_{exchange}_{server}".ToLower();
    }

    public static string GetConnectionKey(string vHost, string server)
    {
        if (regex.Match(vHost).Success == false)
            throw new ValidationException($"The vHost name '{vHost}' is not valid. It should contain only letters, digits, and underscores.");

        if (regex.Match(server).Success == false)
            throw new ValidationException($"The server name '{server}' is not valid. It should contain only letters, digits, and underscores.");

        return $"{vHost}_{server}".ToLower();
    }
}
