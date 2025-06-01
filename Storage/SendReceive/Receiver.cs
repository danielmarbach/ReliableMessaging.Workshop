using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace SendReceive;

public class Receiver(QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<Receiver> logger)
    : IHostedService
{
    private const int MaximumConcurrency = 32;
    private readonly SemaphoreSlim concurrencyLimiter = new(MaximumConcurrency);

    private ConditionalWeakTable<CancellationTokenSource, UpdateReceipt> sourceToPopReceipt = new();
    private CancellationTokenSource cancellationTokenSource;
    private QueueClient receiver;
    private readonly TimeSpan visibilityTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan workTime = TimeSpan.FromSeconds(12);

    [MemberNotNull(nameof(receiver), nameof(cancellationTokenSource))]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        receiver = queueServiceClient.GetQueueClient(configuration.GetSection("Storage")["QueueName"]);
        await receiver.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        cancellationTokenSource = new CancellationTokenSource();

        _ = Task.Run(() => ReceiveMessages(cancellationTokenSource.Token), CancellationToken.None);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await cancellationTokenSource.CancelAsync();
        while (concurrencyLimiter.CurrentCount != MaximumConcurrency)
        {
            await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
        }
    }

    async Task ReceiveMessages(CancellationToken cancellationToken)
    {
        var concurrencyIncrease = 0;
        var emptyReceiveIncrease = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (concurrencyLimiter.CurrentCount == 0)
            {
                concurrencyIncrease++;
                await Task.Delay(concurrencyIncrease * 500, cancellationToken);
                continue;
            }

            concurrencyIncrease = 0;
            QueueMessage[]? messages = await receiver.ReceiveMessagesAsync(maxMessages: concurrencyLimiter.CurrentCount, visibilityTimeout: visibilityTimeout, cancellationToken);
            foreach (var message in messages)
            {
                var coordinationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var coordinationToken = coordinationTokenSource.Token;

                _ = RenewLease(message, coordinationTokenSource, coordinationToken);
                _ = HandleMessage(message, coordinationTokenSource, coordinationToken);
            }

            if (messages.Length == 0)
            {
                emptyReceiveIncrease++;
                await Task.Delay(emptyReceiveIncrease * 500, cancellationToken);
            }
            else
            {
                emptyReceiveIncrease = 0;
            }
        }
    }

    async Task HandleMessage(QueueMessage message, CancellationTokenSource coordinationTokenSource, CancellationToken cancellationToken)
    {
        await concurrencyLimiter.WaitAsync(cancellationToken);

        try
        {
            var envelop = message.Body.ToObjectFromJson<Envelop<ActivateSensor>>(StorageJsonContext.Default.Options)!;

            logger.StartHandling(message.MessageId, envelop.Content.SomeValue);

            // Simulate some work
            await Task.Delay(workTime, cancellationToken);

            if (sourceToPopReceipt.TryGetValue(coordinationTokenSource, out var updateReceipt))
            {
                message = message.Update(updateReceipt);
            }

            await receiver.DeleteMessageAsync(message.MessageId, message.PopReceipt, CancellationToken.None);

            logger.DoneHandling(message.MessageId, envelop.Content.SomeValue);
        }
        catch (OperationCanceledException)
        {
            // try make it visible again
            await receiver.UpdateMessageAsync(message.MessageId, message.PopReceipt, visibilityTimeout: TimeSpan.Zero, cancellationToken: CancellationToken.None);
        }
        catch(Exception ex) when(!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Handling failed");
        }
        finally
        {
            await coordinationTokenSource.CancelAsync();
            coordinationTokenSource.Dispose();
            concurrencyLimiter.Release();
        }
    }

    async Task RenewLease(QueueMessage message, CancellationTokenSource coordinationTokenSource,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(visibilityTimeout.TotalSeconds / 2), cancellationToken);
                UpdateReceipt updateReceipt = await receiver.UpdateMessageAsync(message.MessageId, message.PopReceipt,
                    visibilityTimeout: visibilityTimeout, cancellationToken: cancellationToken);

                message = message.Update(updateReceipt);
                sourceToPopReceipt.AddOrUpdate(coordinationTokenSource, updateReceipt);

                logger.LeaseRenewed(message.MessageId, message.PopReceipt, updateReceipt.PopReceipt);
            }
            catch (OperationCanceledException)
            {
                logger.LeaseRenewalStopped(message.MessageId);
                return;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Renewal failed");
                return;
            }
        }
    }
}