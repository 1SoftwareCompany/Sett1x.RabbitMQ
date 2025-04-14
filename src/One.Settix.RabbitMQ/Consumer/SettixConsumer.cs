﻿using Microsoft.Extensions.Logging;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace One.Settix.RabbitMQ.Consumer;

public sealed class SettixConsumer : AsyncEventingBasicConsumer
{
    private bool isCurrentlyConsuming;

    private readonly ISettixConfigurationMessageProcessor _settixConfigurationMessageProcessor;
    private readonly IChannel _channel;
    private readonly ILogger _logger;

    private const string MessageType = "settix-message-type";

    public SettixConsumer(ISettixConfigurationMessageProcessor settixConfigurationMessageProcessor, IChannel channel, ILogger logger) : base(channel)
    {
        _settixConfigurationMessageProcessor = settixConfigurationMessageProcessor;
        _channel = channel;
        _logger = logger;
        isCurrentlyConsuming = false;
        ReceivedAsync += AsyncListener_Received;
    }

    public async Task ConfigureConsumerAsync(string queueName)
    {
        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.BasicQosAsync(0, 1, false); // prefetch allow to avoid buffer of messages on the flight
            await _channel.BasicConsumeAsync(queueName, false, string.Empty, this); // we should use autoAck: false to avoid messages loosing
        }
    }

    public async Task StopAsync()
    {
        // 1. We detach the listener so ther will be no new messages coming from the queue
        ReceivedAsync -= AsyncListener_Received;

        // 2. Wait to handle any messages in progress
        while (isCurrentlyConsuming)
        {
            // We are trying to wait all consumers to finish their current work.
            // Ofcourse the host could be forcibly shut down but we are doing our best.

            await Task.Delay(10).ConfigureAwait(false);
        }

        if (_channel.IsOpen)
            await _channel.AbortAsync().ConfigureAwait(false);
    }

    private async Task AsyncListener_Received(object sender, BasicDeliverEventArgs configurationMessage)
    {
        try
        {
            _logger.LogDebug("Message received. Sender {sender}.", sender.GetType().Name);
            isCurrentlyConsuming = true;

            if (sender is AsyncEventingBasicConsumer consumer)
                await ProcessAsync(configurationMessage, consumer).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver message");
            throw;
        }
        finally
        {
            isCurrentlyConsuming = false;
        }
    }

    private async Task ProcessAsync(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
    {
        if (ev.BasicProperties.IsHeadersPresent() && ev.BasicProperties.Headers.TryGetValue(MessageType, out object messageType))
        {
            string contract = GetMessageContract(messageType);

            try
            {
                switch (contract)
                {
                    case ConfigureService.ContractId:
                        await ProcessConfigureServiceAsync(ev, consumer).ConfigureAwait(false);
                        break;
                    case ServiceConfigured.ContractId:
                        await ProcessServiceConfiguredAsync(ev, consumer).ConfigureAwait(false);
                        break;
                    case RemoveConfiguration.ContractId:
                        await ProcessRemoveConfigurationAsync(ev, consumer).ConfigureAwait(false);
                        break;
                    case ConfigurationRemoved.ContractId:
                        await ProcessConfigurationRemovedAsync(ev, consumer).ConfigureAwait(false);
                        break;
                    default:
                        _logger.LogError("Mising MessageType {MessageType}, can't desialize message {message}", MessageType, Convert.ToBase64String(ev.Body.ToArray()));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process message. Failed to deserialize : {Convert.ToBase64String(ev.Body.ToArray())}");
            }
        }
        else
        {
            _logger.LogError("Missing MessageType {MessageType}, can't deserialize message {message}", MessageType, Convert.ToBase64String(ev.Body.ToArray()));
        }

        await Ack(ev, consumer).ConfigureAwait(false);

        async Task Ack(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
        {
            if (consumer.Channel.IsOpen)
            {
                await consumer.Channel.BasicAckAsync(ev.DeliveryTag, false).ConfigureAwait(false);
            }
        }
    }

    private static string GetMessageContract(object messageHeader)
    {
        byte[] headerBytes = messageHeader as byte[];
        return Encoding.UTF8.GetString(headerBytes);
    }

    private async Task ProcessConfigureServiceAsync(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
    {
        ConfigureService request = JsonSerializer.Deserialize<ConfigureService>(ev.Body.ToArray());
        await _settixConfigurationMessageProcessor.ProcessAsync(request).ConfigureAwait(false);
    }

    private async Task ProcessServiceConfiguredAsync(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
    {
        ServiceConfigured response = JsonSerializer.Deserialize<ServiceConfigured>(ev.Body.ToArray());
        await _settixConfigurationMessageProcessor.ProcessAsync(response).ConfigureAwait(false);
    }

    private async Task ProcessRemoveConfigurationAsync(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
    {
        RemoveConfiguration request = JsonSerializer.Deserialize<RemoveConfiguration>(ev.Body.ToArray());
        await _settixConfigurationMessageProcessor.ProcessAsync(request).ConfigureAwait(false);
    }

    private async Task ProcessConfigurationRemovedAsync(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
    {
        ConfigurationRemoved response = JsonSerializer.Deserialize<ConfigurationRemoved>(ev.Body.ToArray());
        await _settixConfigurationMessageProcessor.ProcessAsync(response).ConfigureAwait(false);
    }
}
