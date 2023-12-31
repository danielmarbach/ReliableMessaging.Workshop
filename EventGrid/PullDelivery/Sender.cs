using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Options;

namespace PullDelivery;

public class Sender : IHostedService
{
    private readonly EventGridClient eventGridClient;
    private readonly string topicName;

    public Sender(EventGridClient eventGridClient, IOptions<EventGridOptions> eventGridOptions)
    {
        this.eventGridClient = eventGridClient;
        topicName = eventGridOptions.Value.TopicName;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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

        await eventGridClient.PublishCloudEventsAsync(topicName, temperatureChanged, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}