using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Options;

namespace PullDelivery;

public class Sender(EventGridClient eventGridClient, IOptions<EventGridOptions> eventGridOptions)
    : IHostedService
{
    private readonly EventGridClient eventGridClient = eventGridClient;
    private readonly string topicName = eventGridOptions.Value.TopicName;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // TODO
        // 1. Publish Cloud Event with TemperatureChanged Events Inside to a specific source with a custom event type.
        // You can use single event publishes or publish in batches
        // Hint: CloudEvent, The topic and the client are already available
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}