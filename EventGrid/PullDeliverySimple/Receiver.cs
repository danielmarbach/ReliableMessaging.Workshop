using Azure;
using Azure.Core.Serialization;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Options;

namespace PullDelivery;

public class Receiver(
    EventGridReceiverClient eventGridClient,
    ILogger<Receiver> logger,
    IOptions<EventGridOptions> eventGridOptions)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO 2. Stream through the events acknowledge all events where the temperature is below 25
        // keep all the events where the temperature is above 25
        // reject all events that are not from the correct channel
        // try to remove the sender from the Program.cs and see what happens with the events that are released
    }
}