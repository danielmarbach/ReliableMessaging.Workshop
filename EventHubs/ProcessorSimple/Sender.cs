using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Options;

namespace Processor;

public class Sender(IOptions<SenderOptions> senderOptions, EventHubProducerClient eventHubProducerClient)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!senderOptions.Value.ProduceData)
        {
            return;
        }

        foreach (var channel in senderOptions.Value.Channels)
        {
            await foreach (var batch in StreamBatches(
                                   CreateSimulationData(channel, senderOptions.Value.NumberOfDatapointPerChannel),
                                   eventHubProducerClient)
                               .WithCancellation(cancellationToken))
            {
                // TODO 3. Send the batches using the eventHubProducerClient
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    static Queue<EventData> CreateSimulationData(string channel, int numberOfDatapointPerChannel)
    {
        var eventsToSend = new Queue<EventData>();
        var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2));
        for (var i = 0; i < numberOfDatapointPerChannel; i++)
        {
            // TODO 1. Create event data with the necessary temperature changed in the event body
            // Hint: It is possible to use the properties to store the channel for batching
            EventData eventData = null;
            eventsToSend.Enqueue(eventData);
        }

        return eventsToSend;
    }

    static async IAsyncEnumerable<EventDataBatch> StreamBatches(Queue<EventData> queuedEvents,
        EventHubProducerClient producer)
    {
        var currentBatch = default(EventDataBatch);
        while (queuedEvents.Count > 0)
        {
            var eventData = queuedEvents.Peek();

            // TODO 2. Create a batch when currentBatch is null that uses the channel as a partition key to make sure
            // everything from the same channel is ordered. Assign the result to currentBatch

            if (!currentBatch.TryAdd(eventData))
            {
                if (currentBatch.Count == 0)
                {
                    throw new Exception("There was an event too large to fit into a batch.");
                }

                yield return currentBatch;
                currentBatch = default;
            }
            else
            {
                queuedEvents.Dequeue();
            }
        }

        if (currentBatch is { Count: > 0 })
        {
            yield return currentBatch;
        }
    }
}