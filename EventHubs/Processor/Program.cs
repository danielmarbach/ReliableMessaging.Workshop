using System.Collections.Concurrent;
using Azure;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;

var eventHubName = Environment.GetEnvironmentVariable("EVENT_HUB_NAME");
var eventHubConnectionString = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTIONSTRING");
var blobStorageConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTIONSTRING");
var blobContainerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME");
const double TemperatureThreshold = 25;
const int NumberOfDataPointsToObserve = 10;
var channels = new[] { "EE6AFD051F2F", "C67DB8FC86B6", "E70F6A4594B3", "039B202562A2", "4A7C1E9575D6" };
var produceData = true;
var deleteCheckpoint = !produceData;

if (produceData)
{
    await using var producer = new EventHubProducerClient(eventHubConnectionString, eventHubName);

    foreach (var channel in channels)
    {
        await foreach (var batch in StreamBatches(CreateSimulationData(channel), producer))
        {
            using var batchToSend = batch;
            await producer.SendAsync(batchToSend);
        }
    }
}

var storageClient = new BlobContainerClient(
    blobStorageConnectionString,
    blobContainerName);

if (deleteCheckpoint)
{
    try
    {
        await storageClient.DeleteAsync();
    }
    catch (RequestFailedException ex) when(ex.ErrorCode == "ContainerNotFound")
    {
    }
}

RequestFailedException? exception = null;
do
{
    try
    {
        await storageClient.CreateIfNotExistsAsync();
        exception = null;
    }
    catch (RequestFailedException ex)
    {
        exception = ex;
        Console.Write(".");
        await Task.Delay(500);
    }
} while (exception != null);

var checkpointStore = new BlobCheckpointStore(storageClient);
var maximumBatchSize = 100;

var channelObservations = new ConcurrentDictionary<string, int>();

var processor = new BatchProcessor(checkpointStore, maximumBatchSize, "$Default", eventHubConnectionString, eventHubName);
processor.ProcessEventAsync += async events =>
{
    foreach (var @event in events)
    {
        var temperatureChanged = @event.EventBody.ToObjectFromJson<TemperatureChanged>();
        var channel = @event.Properties["Channel"].ToString()!;

        var numberOfDataPointsObserved = channelObservations.AddOrUpdate(channel,
            static (_, _) => 0,
            static (_, points, current) => current > TemperatureThreshold ? points + 1 : 0,
            temperatureChanged.Current);

        if (numberOfDataPointsObserved < NumberOfDataPointsToObserve)
        {
            await Console.Out.WriteLineAsync($" - {channel}: {temperatureChanged.Current} / {temperatureChanged.Published}{(numberOfDataPointsObserved == 0 ? $" (below threshold of '{TemperatureThreshold}', reset)": string.Empty)}");
        }
        else
        {
            await Console.Out.WriteLineAsync($" - {channel}: {temperatureChanged.Current} / {temperatureChanged.Published} (above threshold of '{TemperatureThreshold}' for the last '{numberOfDataPointsObserved}' data points)");
        }
    }
};

using var cancellationSource = new CancellationTokenSource();
cancellationSource.CancelAfter(TimeSpan.FromSeconds(60));

await processor.StartProcessingAsync(cancellationSource.Token);
await Task.Delay(Timeout.Infinite, cancellationSource.Token).ContinueWith(t => t, TaskContinuationOptions.None);
await processor.StopProcessingAsync();

static Queue<EventData> CreateSimulationData(string channel)
{
    var eventsToSend = new Queue<EventData>();
    var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2));
    for (var i = 0; i < 10000; i++)
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
                { "Channel", channel}
            }
        };
        eventsToSend.Enqueue(eventData);
    }

    return eventsToSend;
}

static async IAsyncEnumerable<EventDataBatch> StreamBatches(Queue<EventData> queuedEvents, EventHubProducerClient producer)
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

public class TemperatureChanged
{
    public DateTimeOffset Published { get; init; }
    public double Current { get; init; }
};