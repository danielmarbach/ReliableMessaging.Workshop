using Azure;
using Azure.Core.Serialization;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Options;

namespace PullDelivery;

public class Receiver(
    EventGridClient eventGridClient,
    ILogger<Receiver> logger,
    IOptions<EventGridOptions> eventGridOptions)
    : BackgroundService
{
    private readonly string subscriptionName = eventGridOptions.Value.SubscriptionName;
    private readonly string topicName = eventGridOptions.Value.TopicName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var toRelease = new List<string>();
        var toAcknowledge = new List<string>();
        var toReject = new List<string>();

        await foreach (var detail in eventGridClient.StreamCloudEvents(topicName, subscriptionName, maxEvents: 100, maxWaitTime: TimeSpan.FromSeconds(10), stoppingToken))
        {
            var @event = detail.Event;
            var brokerProperties = detail.BrokerProperties;

            switch (@event.Source)
            {
                case "ChannelA" when
                    await @event.Data!.ToObjectAsync<TemperatureChanged>(JsonObjectSerializer.Default, cancellationToken: stoppingToken) is { Current: > 25 } warm:
                    toRelease.Add(brokerProperties.LockToken);
                    logger.TemperatureChanged(warm.Current, warm.Published);
                    break;
                case "ChannelA":
                    toAcknowledge.Add(brokerProperties.LockToken);
                    break;
                default:
                    toReject.Add(brokerProperties.LockToken);
                    break;
            }
        }

        if (toRelease.Count > 0)
        {
            logger.Releasing(toRelease.Count);
            // ignoring the result because it is a demo
            _ = await eventGridClient.ReleaseCloudEventsAsync(topicName, subscriptionName, toRelease, stoppingToken);
        }

        if (toAcknowledge.Count > 0)
        {
            logger.Acknowledge(toAcknowledge.Count);
            // ignoring the result because it is a demo
            _ = await eventGridClient.AcknowledgeCloudEventsAsync(topicName, subscriptionName, toAcknowledge, stoppingToken);
        }

        if (toReject.Count > 0)
        {
            logger.Reject(toReject.Count);
            // ignoring the result because it is a demo
            _ = await eventGridClient.RejectCloudEventsAsync(topicName, subscriptionName, toReject, stoppingToken);
        }
    }
}