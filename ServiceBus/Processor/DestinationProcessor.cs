using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace Processor;

public class DestinationProcessor(
    [FromKeyedServices("Client")] ServiceBusClient serviceBusClient,
    IOptions<ServiceBusOptions> serviceBusOptions,
    ILogger<DestinationProcessor> logger)
    : IHostedService, IAsyncDisposable
{
    private ServiceBusProcessor? queueProcessor;
    // Of course, an ever-growing dictionary is not a good idea, this data structure usage
    // is for demonstration purposes only.
    readonly ConcurrentDictionary<string, bool> receivedMessageIds = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        queueProcessor = serviceBusClient.CreateProcessor(serviceBusOptions.Value.DestinationQueue, new ServiceBusProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = 10,
            AutoCompleteMessages = true,
            PrefetchCount = 10,
            Identifier = $"Processor-{serviceBusOptions.Value.DestinationQueue}",
        });
        queueProcessor.ProcessMessageAsync += ProcessMessages;
        queueProcessor.ProcessErrorAsync += ProcessError;
        await queueProcessor.StartProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessages(ProcessMessageEventArgs arg)
    {
        arg.Message.ApplicationProperties.TryGetValue("MessageType", out var messageTypeValue);
        var handlerTask = messageTypeValue switch
        {
            "Processor.SensorActivated" => HandleSensorActivated(arg.Message, arg.CancellationToken),
            _ => Task.CompletedTask
        };
        await handlerTask;
    }

    Task HandleSensorActivated(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var alreadyReceived = receivedMessageIds.AddOrUpdate(message.CorrelationId, static _ => false, static (_, _) => true);
        logger.SensorActivated(alreadyReceived ? LogLevel.Warning : LogLevel.Information, message.CorrelationId);
        return Task.CompletedTask;
    }

    private Task ProcessError(ProcessErrorEventArgs arg)
    {
        if (arg.Exception is OperationCanceledException)
        {
            return Task.CompletedTask;
        }
        logger.LogError(arg.Exception, "Error processing message");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (queueProcessor is not null)
        {
            await queueProcessor.StopProcessingAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (queueProcessor is not null)
        {
            await queueProcessor.DisposeAsync();
        }
    }
}