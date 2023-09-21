using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;

public class BatchProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>
{
    public BatchProcessor(CheckpointStore checkpointStore,
        int eventBatchMaximumCount,
        string consumerGroup,
        string connectionString,
        string eventHubName,
        EventProcessorOptions? clientOptions = default)
        : base(
            checkpointStore,
            eventBatchMaximumCount,
            consumerGroup,
            connectionString,
            eventHubName,
            clientOptions)
    {
    }

    public Func<IReadOnlyList<EventData>, Task> ProcessEventAsync { get; set; } = data => Task.CompletedTask;

    protected override async Task OnProcessingEventBatchAsync(IEnumerable<EventData> events,
        EventProcessorPartition partition,
        CancellationToken cancellationToken)
    {
        try
        {
            var readOnlyList = (IReadOnlyList<EventData>)events;

            await Console.Out.WriteLineAsync(
                $"Received batch of {readOnlyList.Count} event on partition {partition.PartitionId}");

            await ProcessEventAsync(readOnlyList);
            
            var lastEvent = readOnlyList[^1];

            await UpdateCheckpointAsync(
                partition.PartitionId,
                lastEvent.Offset,
                lastEvent.SequenceNumber,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error processing partition '{partition.PartitionId} due to {ex.Message}");
        }
    }

    protected override async Task OnProcessingErrorAsync(Exception exception,
        EventProcessorPartition partition,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        await Console.Error.WriteLineAsync($"Error processing partition '{partition.PartitionId} with '{operationDescription}' due to {exception.Message}");
    }
}