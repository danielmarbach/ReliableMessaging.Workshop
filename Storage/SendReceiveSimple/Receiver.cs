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

            QueueMessage[]? messages = Array.Empty<QueueMessage>();
            // TODO
            // 5. Receive as many messages as needed
            // HINT: concurrencyLimiter.CurrentCount
            foreach (var message in messages)
            {
                var coordinationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _ = RenewLease(message, coordinationTokenSource, coordinationTokenSource.Token);
                _ = HandleMessage(message, coordinationTokenSource, cancellationToken);
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
            // TODO
            // 6. Deserialize the Envelop with the ActivateSensor data

            // Simulate some work
            await Task.Delay(workTime, cancellationToken);

            if (sourceToPopReceipt.TryGetValue(coordinationTokenSource, out var updateReceipt))
            {
                message = message.Update(updateReceipt);
            }

            // TODO
            // 7. Mark the message as processed
        }
        catch (OperationCanceledException)
        {
            // TODO
            // 8. When processing failed during shutdown try to make it immediately visible again.
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

                UpdateReceipt updateReceipt = null;
                // TODO
                // 9. Update the visibility time of the message

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