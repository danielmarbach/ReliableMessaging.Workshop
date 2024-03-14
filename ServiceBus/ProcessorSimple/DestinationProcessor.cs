using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Processor;

public class DestinationProcessor(
    IAzureClientFactory<ServiceBusClient> clientFactory,
    IOptions<ServiceBusOptions> serviceBusOptions,
    ILogger<DestinationProcessor> logger)
    : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient = clientFactory.CreateClient("Client");
    private ServiceBusProcessor? queueProcessor;
    private long sensorActivatedCounter = 0;

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
        // TODO
        // 12. Extract the MessageType from the ApplicationProperties and call HandleSensorActivated if the type matches
        // 13. otherwise consume the message
    }

    Task HandleSensorActivated(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var sensorActivated = Interlocked.Increment(ref sensorActivatedCounter);
        logger.OrderAccepted(sensorActivated <= serviceBusOptions.Value.NumberOfCommands ? LogLevel.Information : LogLevel.Warning, sensorActivated);
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