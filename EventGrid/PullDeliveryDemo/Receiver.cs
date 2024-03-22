using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.Namespaces;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace PullDeliveryDemo;

public class Receiver(
    EventGridClient eventGridClient,
    BlobContainerClient blobContainerClient,
    ILogger<Receiver> logger,
    IOptions<EventGridOptions> eventGridOptions)
    : BackgroundService
{
    private readonly string subscriptionName = eventGridOptions.Value.SubscriptionName;
    private readonly string topicName = eventGridOptions.Value.TopicName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        await foreach (var detail in eventGridClient.StreamCloudEvents(topicName, subscriptionName, maxEvents: 100, maxWaitTime: TimeSpan.FromSeconds(10), stoppingToken))
        {
            var @event = detail.Event;
            var brokerProperties = detail.BrokerProperties;

            if(@event.TryGetSystemEventData(out var systemEvent))
            {
                switch (systemEvent)
                {
                    case StorageBlobCreatedEventData blobCreatedEventData:
                        await DownloadAndLog(blobCreatedEventData, stoppingToken);
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

            await eventGridClient.AcknowledgeCloudEventsAsync(topicName, subscriptionName, [brokerProperties.LockToken], stoppingToken);
        }
    }

    private async Task DownloadAndLog(StorageBlobCreatedEventData blobCreatedEventData, CancellationToken stoppingToken = default)
    {
        var blobName = blobCreatedEventData.Url.Split('/').Last();

        var downloaded = await blobContainerClient.GetBlobClient(blobName).DownloadAsync(stoppingToken);
        await using var contentStream = downloaded.Value.Content;
        using var reader = new StreamReader(contentStream);
        logger.BlobDownloaded(blobName, await reader.ReadToEndAsync(stoppingToken));
    }
}