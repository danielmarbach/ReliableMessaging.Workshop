using Azure;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;

var eventHubName = Environment.GetEnvironmentVariable("EVENT_HUB_NAME");
var eventHubConnectionString = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTIONSTRING");
var blobStorageConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTIONSTRING");
var blobContainerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME");
var produceData = true;
var deleteCheckpoint = !produceData;

if (produceData)
{
    await using var producer = new EventHubProducerClient(eventHubConnectionString, eventHubName);

    await foreach (var batch in StreamBatches(CreateSimulationData(), producer))
    {
        using var batchToSend = batch;
        await producer.SendAsync(batchToSend);
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

var temperatureThreshold = 25;
var numberOfDataPointsToObserve = 10;
var numberOfDataPointsObserved = 0;
var processor = new BatchProcessor(checkpointStore, maximumBatchSize, "$Default", eventHubConnectionString, eventHubName);
processor.ProcessEventAsync += async events =>
{
    foreach (var @event in events)
    {
        var temperatureChanged = @event.EventBody.ToObjectFromJson<TemperatureChanged>();
        Console.WriteLine($" - {temperatureChanged.Current} / {temperatureChanged.Published}");
        if (temperatureChanged.Current > temperatureThreshold)
        {
            numberOfDataPointsObserved++;
        }
        else
        {
            numberOfDataPointsObserved = 0;
        }

        if (numberOfDataPointsObserved < numberOfDataPointsToObserve)
        {
            continue;
        }

        await Console.Out.WriteLineAsync(
            "==============================================================================");
        await Console.Out.WriteLineAsync(
            $"The temperature was above the threshold of {temperatureThreshold} for the last {numberOfDataPointsObserved}");
        await Console.Out.WriteLineAsync(
            "==============================================================================");
    }
};

using var cancellationSource = new CancellationTokenSource();
cancellationSource.CancelAfter(TimeSpan.FromSeconds(30));

await processor.StartProcessingAsync(cancellationSource.Token);
await Task.Delay(Timeout.Infinite, cancellationSource.Token).ContinueWith(t => t, TaskContinuationOptions.None);
await processor.StopProcessingAsync();

static Queue<EventData> CreateSimulationData()
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
        currentBatch ??= await producer.CreateBatchAsync();
        var eventData = queuedEvents.Peek();

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