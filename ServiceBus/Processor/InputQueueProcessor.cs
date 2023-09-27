using System.Transactions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Processor;

public class InputQueueProcessor : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;
    private readonly ILogger<Sender> logger;
    private ServiceBusProcessor? queueProcessor;

    public InputQueueProcessor(IAzureClientFactory<ServiceBusClient> clientFactory, IOptions<ServiceBusOptions> serviceBusOptions, ILogger<Sender> logger)
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
        await using var publisher = serviceBusClient.CreateSender(serviceBusOptions.Value.TopicName, new ServiceBusSenderOptions
        {
            Identifier = $"Publisher-{serviceBusOptions.Value.TopicName}"
        });

        // will make sure operations will enlist
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var submitOrder = message.Body.ToObjectFromJson<SubmitOrder>();
        logger.SubmitOrderReceived(submitOrder.OrderId);

        try
        {
            var orderAccepted = new OrderAccepted
            {
                OrderId = submitOrder.OrderId
            };
            var orderAcceptedMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(orderAccepted))
            {
                ContentType = "application/json",
                CorrelationId = message.MessageId,
                ApplicationProperties =
                {
                    { "MessageType", typeof(OrderAccepted).FullName }
                }
            };
            
            // the order here doesn't matter because the message will only go out if the rest was successful
            await publisher.SendMessageAsync(orderAcceptedMessage, cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 15)), cancellationToken);
            
            scope.Complete();
        }
        catch (OperationCanceledException e)
        {
            logger.SubmitOrderLockLost(e, submitOrder.OrderId);
            throw;
        }
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