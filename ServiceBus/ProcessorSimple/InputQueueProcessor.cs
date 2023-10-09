using System.Transactions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Processor;

public class InputQueueProcessor : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;
    private readonly ILogger<InputQueueProcessor> logger;
    private ServiceBusProcessor? queueProcessor;
    private ServiceBusSender? publisher;

    public InputQueueProcessor(IAzureClientFactory<ServiceBusClient> clientFactory, IOptions<ServiceBusOptions> serviceBusOptions, ILogger<InputQueueProcessor> logger)
    {
        serviceBusClient = clientFactory.CreateClient("TransactionalClient");
        this.serviceBusOptions = serviceBusOptions;
        this.logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        queueProcessor = serviceBusClient.CreateProcessor(serviceBusOptions.Value.InputQueue, new ServiceBusProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = 10,
            AutoCompleteMessages = true,
            PrefetchCount = 10,
            Identifier = $"Processor-{serviceBusOptions.Value.InputQueue}",
            MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(10),
        });
        queueProcessor.ProcessMessageAsync += ProcessMessages;
        queueProcessor.ProcessErrorAsync += ProcessError;
        publisher = serviceBusClient.CreateSender(serviceBusOptions.Value.TopicName, new ServiceBusSenderOptions
        {
            Identifier = $"Publisher-{serviceBusOptions.Value.TopicName}"
        });

        await queueProcessor.StartProcessingAsync(cancellationToken);
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

    private async Task ProcessMessages(ProcessMessageEventArgs arg)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(arg.CancellationToken);
        try
        {
            arg.MessageLockLostAsync += MessageLockLostHandler;

            arg.Message.ApplicationProperties.TryGetValue("MessageType", out var messageTypeValue);
            var handlerTask = messageTypeValue switch
            {
                "Processor.ActivateSensor" => HandleActivateSensor(arg.Message, cts.Token),
                "Processor.SensorActivated" => HandleSensorActivated(arg.Message, cts.Token),
                _ => Task.CompletedTask
            };
            await handlerTask;
        }
        finally
        {
            arg.MessageLockLostAsync -= MessageLockLostHandler;
        }

        Task MessageLockLostHandler(MessageLockLostEventArgs lockLostArgs)
        {
            logger.LogInformation(lockLostArgs.Exception, "Lost the lock while processing message. Cancelling the handler");
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignored
                logger.LogCritical(lockLostArgs.Exception, "Lock lost handler executed but cancellation token source was already disposed.");
            }
            return Task.CompletedTask;
        }
    }

    async Task HandleActivateSensor(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        // TODO
        // Wrap everything in a transaction scope with async flow options enabled
        // Deserialize the body into an ActivateSensor instance
        // Create a SensorActivated type and create a new ServiceBusMessage with that content
        // Set the CorrelationId to the original message ID
        // Set the application properties MessageType to the SensorActivated type
        // Publish the event with the "publisher"  sender and eventually complete the transaction scope
        // will make sure operations will enlist

        // here to simulate some work
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 15)), cancellationToken);
    }

    Task HandleSensorActivated(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        logger.SensorActivatedWithSubject(message.Subject);
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

        if (publisher is not null)
        {
            await publisher.DisposeAsync();
        }
    }
}