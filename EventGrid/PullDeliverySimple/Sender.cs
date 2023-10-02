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
        // TODO
        // 1. Publish Cloud Event with TemperatureChanged Events Inside. You can use single event publishes
        // or publish in batches
        // Hint: CloudEvent, The topic and the client are already available
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}