using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Options;

namespace Processor;

public class Sender : IHostedService
{
    private readonly IOptions<SenderOptions> senderOptions;
    private readonly EventHubProducerClient eventHubProducerClient;

    public Sender(IOptions<SenderOptions> senderOptions, EventHubProducerClient eventHubProducerClient)
    {
        this.senderOptions = senderOptions;
        this.eventHubProducerClient = eventHubProducerClient;
    }

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
                using var batchToSend = batch;
                await eventHubProducerClient.SendAsync(batchToSend, cancellationToken);
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
            var eventData = new EventData
            {
                ContentType = "application/json",
                EventBody = BinaryData.FromObjectAsJson(new TemperatureChanged
                {
                    Published = yesterday.Add(TimeSpan.FromSeconds(i)),
                    Current = Random.Shared.Next(20, 30) + Random.Shared.NextDouble()
                }),
                Properties =
                {
                    { "Channel", channel }
                }
            };
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

            currentBatch ??= await producer.CreateBatchAsync(new CreateBatchOptions
            {
                PartitionKey = eventData.Properties["Channel"].ToString()
            });

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