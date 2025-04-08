using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using One.Settix.RabbitMQ.Bootstrap;
using One.Settix.RabbitMQ.Consumer;
using One.Settix.RabbitMQ.Publisher;

namespace One.Settix.RabbitMQ;

public static class SettixRabbitMqExtensions
{
    // TODO: Rethink all lifecycles of the services
    internal static IServiceCollection AddSettixRabbitMqBase(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<SettixRabbitMqStartup>();
        services.AddSingleton<ConnectionResolver>();

        return services;
    }

    public static IServiceCollection AddSettixRabbitMqPublisher(this IServiceCollection services)
    {
        services.AddSettixRabbitMqBase();

        services.AddOptions<RabbitMqClusterOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetRequiredSection("settix:rabbitmq:publisher").Bind(options.Clusters);
        });

        services.AddSingleton<PublisherChannelResolver>();
        services.AddSingleton<SettixRabbitMqPublisher>();

        return services;
    }

    public static IServiceCollection AddSettixRabbitMqConsumer(this IServiceCollection services)
    {
        services.AddSettixRabbitMqBase();

        services.AddOptions<RabbitMqOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetRequiredSection("settix:rabbitmq:consumer").Bind(options);
        });

        services.AddSingleton<ConsumerPerQueueChannelResolver>();
        services.AddSingleton<SettixRabbitMqConsumerFactory>();

        return services;
    }
}

