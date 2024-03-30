using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.Data.SchemaRegistry.ApacheAvro;
using Microsoft.Extensions.Options;

namespace ProcessorSchemaDemo;

public class Sender(IOptions<SenderOptions> senderOptions, EventHubProducerClient eventHubProducerClient, SchemaRegistryAvroSerializer serializer)
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
                                   await CreateSimulationData(channel, senderOptions.Value.NumberOfDatapointPerChannel, serializer),
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

    static async ValueTask<Queue<EventData>> CreateSimulationData(string channel, int numberOfDatapointPerChannel, SchemaRegistryAvroSerializer serializer)
    {
        var eventsToSend = new Queue<EventData>();
        var yesterday = DateTime.UtcNow.Subtract(TimeSpan.FromDays(2));
        for (var i = 0; i < numberOfDatapointPerChannel; i++)
        {
            var eventData = await serializer.SerializeAsync<EventData, TemperatureChanged>(new TemperatureChanged
            {
                Published = yesterday.Add(TimeSpan.FromSeconds(i)),
                Current = Random.Shared.Next(20, 30) + Random.Shared.NextDouble()
            });
            eventData.Properties["Channel"] = channel;
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