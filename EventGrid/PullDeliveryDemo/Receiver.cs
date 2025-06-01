using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.Namespaces;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;

namespace PullDeliveryDemo;

public class Receiver(
    EventGridReceiverClient eventGridClient,
    BlobContainerClient blobContainerClient,
    ILogger<Receiver> logger)
    : BackgroundService
{
    private readonly TimeSpan visibilityTimeout = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var detail in eventGridClient.StreamCloudEvents(maxEvents: 100, maxWaitTime: TimeSpan.FromSeconds(10), stoppingToken))
        {
            var @event = detail.Event;
            var brokerProperties = detail.BrokerProperties;
            string[] lockTokens = [brokerProperties.LockToken];

            if(@event.TryGetSystemEventData(out var systemEvent))
            {
                switch (systemEvent)
                {
                    case StorageBlobCreatedEventData blobCreatedEventData:
                        var coordinationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        var coordinationToken = coordinationTokenSource.Token;

                        _ = RenewLocks(lockTokens, coordinationToken);
                        await DownloadAndLog(blobCreatedEventData, coordinationTokenSource, coordinationToken);
                        break;
                    case StorageBlobDeletedEventData blobDeletedEventData:
                        logger.LogInformation("Blob deleted: {BlobUrl}", blobDeletedEventData.Url);
                        break;
                }
            }
            else
            {
                logger.LogInformation("Received event: {EventType}", @event.Type);
            }

            await eventGridClient.AcknowledgeAsync(lockTokens, stoppingToken);
        }
    }

    private async Task RenewLocks(string[] lockTokens, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(visibilityTimeout.TotalSeconds / 2), cancellationToken);
                RenewLocksResult result = await eventGridClient.RenewLocksAsync(lockTokens, cancellationToken);

                logger.LocksRenewed(lockTokens, result.SucceededLockTokens, result.FailedLockTokens.Count > 0 ? result.FailedLockTokens.Select(f => f.LockToken) : [], result);
            }
            catch (OperationCanceledException)
            {
                logger.LeaseRenewalStopped(lockTokens);
                return;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Renewal failed");
                return;
            }
        }
    }

    private async Task DownloadAndLog(StorageBlobCreatedEventData blobCreatedEventData,
        CancellationTokenSource coordinationTokenSource, CancellationToken stoppingToken = default)
    {
        try
        {
            var blobName = blobCreatedEventData.Url.Split('/').Last();

            var blobClient = blobContainerClient.GetBlobClient(blobName);
            if (await blobClient.ExistsAsync(stoppingToken))
            {
                var downloaded = await blobClient.DownloadAsync(stoppingToken);
                await using var contentStream = downloaded.Value.Content;
                using var reader = new StreamReader(contentStream);
                logger.BlobDownloaded(blobName, await reader.ReadToEndAsync(stoppingToken));
            }
        }
        finally
        {
            await coordinationTokenSource.CancelAsync();
            coordinationTokenSource.Dispose();
        }
    }
}