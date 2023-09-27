using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Processor;

public class DestinationProcessor : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;
    private readonly ILogger<Sender> logger;
    private ServiceBusProcessor queueProcessor;
    private long orderAcceptedCounter = 0;

    public DestinationProcessor(IAzureClientFactory<ServiceBusClient> clientFactory, IOptions<ServiceBusOptions> serviceBusOptions, ILogger<Sender> logger)
    {
        this.serviceBusClient = clientFactory.CreateClient("Client");
        this.serviceBusOptions = serviceBusOptions;
        this.logger = logger;
    }
    
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
            "Processor.OrderAccepted" => HandleOrderAccepted(arg.Message, arg.CancellationToken),
            _ => Task.CompletedTask
        };
        await handlerTask;
    }
    
    Task HandleOrderAccepted(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var orderAccepted = Interlocked.Increment(ref orderAcceptedCounter);
        logger.Log(orderAccepted < serviceBusOptions.Value.NumberOfCommands ? LogLevel.Information : LogLevel.Warning, $"#{orderAccepted} have been accepted");
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
        await queueProcessor.StopProcessingAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await queueProcessor.DisposeAsync();
    }
}