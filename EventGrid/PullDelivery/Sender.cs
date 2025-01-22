using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Options;

namespace PullDelivery;

public class Sender(EventGridSenderClient eventGridClient)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var temperatureChanged = new List<CloudEvent>();
        var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
        for (var i = 0; i < 100; i++)
        {
            temperatureChanged.Add(new CloudEvent("ChannelA", "ReliableMessaging.Workshop.TemperatureChanged",
                new TemperatureChanged
                {
                    Published = yesterday.Add(TimeSpan.FromMinutes(i)),
                    Current = Random.Shared.Next(20, 30) + Random.Shared.NextDouble()
                }));
        }

        await eventGridClient.SendAsync(temperatureChanged, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}