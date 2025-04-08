using Microsoft.Extensions.Logging;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace One.Settix.RabbitMQ.Consumer;

public sealed class AsyncConsumer : AsyncEventingBasicConsumer
{
    private bool isСurrentlyConsuming;

    private readonly ISettixConfigurationMessageProcessor _settixConfigurationMessageProcessor;
    private readonly IModel _model;
    private readonly ILogger _logger;

    private const string MessageType = "settix-message-type";

    public AsyncConsumer(string queuName, ISettixConfigurationMessageProcessor settixConfigurationMessageProcessor, IModel model, ILogger logger) : base(model)
    {
        model.BasicQos(0, 1, false); // prefetch allow to avoid buffer of messages on the flight
        model.BasicConsume(queuName, false, string.Empty, this); // we should use autoAck: false to avoid messages loosing

        _settixConfigurationMessageProcessor = settixConfigurationMessageProcessor;
        _model = model;
        _logger = logger;
        isСurrentlyConsuming = false;
        Received += AsyncListener_Received;
    }

    public async Task StopAsync()
    {
        // 1. We detach the listener so ther will be no new messages coming from the queue
        Received -= AsyncListener_Received;

        // 2. Wait to handle any messages in progress
        while (isСurrentlyConsuming)
        {
            // We are trying to wait all consumers to finish their current work.
            // Ofcourse the host could be forcibly shut down but we are doing our best.

            await Task.Delay(10).ConfigureAwait(false);
        }

        if (_model.IsOpen)
            _model.Abort();
    }

    private async Task AsyncListener_Received(object sender, BasicDeliverEventArgs @event)
    {
        try
        {
            _logger.LogDebug("Message received. Sender {sender}.", sender.GetType().Name);
            isСurrentlyConsuming = true;

            if (sender is AsyncEventingBasicConsumer consumer)
                await ProcessAsync(@event, consumer).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver message");
            throw;
        }
        finally
        {
            isСurrentlyConsuming = false;
        }
    }

    private async Task ProcessAsync(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
    {
        bool isRequest = false;

        if (ev.BasicProperties.IsHeadersPresent() && ev.BasicProperties.Headers.TryGetValue(MessageType, out object messageType))
        {
            isRequest = IsRequestMessage(messageType);
        }
        else
        {
            _logger.LogError("Miising MessageType {MessageType}, can't desialize message {message}", MessageType, Convert.ToBase64String(ev.Body.ToArray()));
            Ack(ev, consumer);
            return;
        }

        ConfigurationRequest request = default; // this is not ideal
        ConfigurationResponse response = default; // this is not ideal
        try
        {
            if (isRequest)
            {
                request = JsonSerializer.Deserialize<ConfigurationRequest>(ev.Body.ToArray());
            }
            else
            {
                response = JsonSerializer.Deserialize<ConfigurationResponse>(ev.Body.ToArray());
            }
        }
        catch (Exception ex)
        {
            // TODO: send to dead letter exchange/queue
            _logger.LogError(ex, $"Failed to process message. Failed to deserialize : {Convert.ToBase64String(ev.Body.ToArray())}");
            Ack(ev, consumer);
            return;
        }

        try
        {
            if (isRequest)
            {
                await _settixConfigurationMessageProcessor.ProcessAsync(request).ConfigureAwait(false);
                _logger.LogDebug("Received request message: {@message}", request);
            }
            else
            {
                await _settixConfigurationMessageProcessor.ProcessAsync(response).ConfigureAwait(false);
                _logger.LogDebug("Received response message: {@message}", response);
            }

        }
        catch (Exception ex)
        {
            if (isRequest)
                _logger.LogError(ex, "Failed to process request message. {message}", JsonSerializer.Serialize(request));
            else
                _logger.LogError(ex, "Failed to process response message. {message}", JsonSerializer.Serialize(response));
        }
        finally
        {
            // Acknowledge the message
            Ack(ev, consumer);
        }

        static void Ack(BasicDeliverEventArgs ev, AsyncEventingBasicConsumer consumer)
        {
            if (consumer.Model.IsOpen)
            {
                consumer.Model.BasicAck(ev.DeliveryTag, false);
            }
        }
    }

    private static bool IsRequestMessage(object messageHeader)
    {
        if (messageHeader is byte[] byteArray)
        {
            ReadOnlySpan<byte> byteArrAsSpan = byteArray.AsSpan();
            return byteArrAsSpan.IndexOf(Encoding.UTF8.GetBytes(ConfigurationRequest.ContractId).AsSpan()) > -1;
        }
        return false;
    }
}
