using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace Processor;

public class Processor : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;
    private readonly ILogger<Sender> logger;
    private ServiceBusProcessor queueProcessor;

    public Processor(ServiceBusClient serviceBusClient, IOptions<ServiceBusOptions> serviceBusOptions, ILogger<Sender> logger)
    {
        this.serviceBusClient = serviceBusClient;
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
                "Processor.SubmitOrder" => HandleSubmitOrder(arg.Message, cts.Token),
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

    async Task HandleSubmitOrder(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var submitOrder = message.Body.ToObjectFromJson<SubmitOrder>();
        logger.LogInformation($"SubmitOrder command for ID {submitOrder.OrderId} received");

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 15)), cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            logger.LogError(e, $"Lost the lock while processing ID {submitOrder.OrderId}");
            throw;
        }
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