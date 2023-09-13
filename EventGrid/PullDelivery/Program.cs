// See https://aka.ms/new-console-template for more information

using Azure;
using Azure.Core.Serialization;
using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;
using PullDelivery;

var topicName = "TemperatureChanged";
var subscriptionName = "TemperatureChangedSubscription";
var topicEndpoint = Environment.GetEnvironmentVariable("TOPIC_ENDPOINT");
var accessKey = Environment.GetEnvironmentVariable("TOPIC_ACCESS_KEY");

var client = new EventGridClient(new Uri(topicEndpoint), new AzureKeyCredential(accessKey));

var temperatureChanged = new List<CloudEvent>();
var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
for (var i = 0; i < 100; i++)
{
    temperatureChanged.Add(new("ChannelA", "ReliableMessaging.Workshop.TemperatureChanged",
        new TemperatureChanged
        {
            Published = yesterday.Add(TimeSpan.FromMinutes(i)),
            Current = Random.Shared.Next(20, 30) + Random.Shared.NextDouble()
        }));
}

await client.PublishCloudEventsAsync(topicName, temperatureChanged);

var toRelease = new List<string>();
var toAcknowledge = new List<string>();
var toReject = new List<string>();

var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await foreach (var detail in client.StreamCloudEvents(topicName, subscriptionName, maxEvents: 100, maxWaitTime: TimeSpan.FromSeconds(10), token: cancellationTokenSource.Token))
{
    CloudEvent @event = detail.Event;
    BrokerProperties brokerProperties = detail.BrokerProperties;

    if (@event.Source == "ChannelA" &&
        await @event.Data.ToObjectAsync<TemperatureChanged>(JsonObjectSerializer.Default) is { Current: > 25 } warm)
    {
        toRelease.Add(brokerProperties.LockToken);
        Console.WriteLine($" - {warm.Current} / {warm.Published}");
    }
    else if (@event.Source == "ChannelA")
    {
        toAcknowledge.Add(brokerProperties.LockToken);
    }
    else
    {
        toReject.Add(brokerProperties.LockToken);
    }
}

if (toRelease.Count > 0)
{
    Console.WriteLine($"Releasing {toRelease.Count}");
    // ignoring the result because it is a demo
    _ = await client.ReleaseCloudEventsAsync(topicName, subscriptionName, toRelease);
}

if (toAcknowledge.Count > 0)
{
    Console.WriteLine($"Acknowledge {toAcknowledge.Count}");
    // ignoring the result because it is a demo
    _ = await client.AcknowledgeCloudEventsAsync(topicName, subscriptionName, toRelease);
}

if (toReject.Count > 0)
{
    Console.WriteLine($"Reject {toReject.Count}");
    // ignoring the result because it is a demo
    _ = await client.RejectCloudEventsAsync(topicName, subscriptionName, toRelease);
}

public class TemperatureChanged
{
    public DateTimeOffset Published { get; init; }
    public double Current { get; init; }
};