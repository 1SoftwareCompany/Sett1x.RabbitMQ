using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.Bootstrap.Management;
using One.Settix.RabbitMQ.Consumer;
using One.Settix.RabbitMQ.Publisher;
using System.Net.Http.Headers;
using System.Text;

namespace One.Settix.RabbitMQ;

public static class SettixRabbitMqExtensions
{
    internal static IServiceCollection AddSettixRabbitMqBase(this IServiceCollection services)
    {
        services.AddSingleton<SettixRabbitMqConnectionFactory>();
        services.AddSingleton<SettixRabbitMqConfiguration>();
        services.AddSingleton<ConnectionResolver>();
        services.AddSingleton<SettixRabbitMqConsumerFactory>();

        return services;
    }

    public static IServiceCollection AddSettix(this IServiceCollection services)
    {
        services.AddSettixRabbitMqBase();

        services.AddOptions<RabbitMqClusterOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetRequiredSection("settix:rabbitmq:publisher").Bind(options.Clusters);
        });

        services.AddSingleton<PublisherChannelResolver>();
        services.AddSingleton<SettixPublisher>();

        services.AddOptions<RabbitMqOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetRequiredSection("settix:rabbitmq:consumer").Bind(options);
        });

        services.AddSingleton<ConsumerPerQueueChannelResolver>();

        return services;
    }
}
