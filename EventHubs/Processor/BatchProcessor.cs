using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;

namespace Processor;

public sealed class BatchProcessor(
    CheckpointStore checkpointStore,
    int eventBatchMaximumCount,
    string consumerGroup,
    string connectionString,
    string eventHubName,
    ILogger<BatchProcessor> logger,
    EventProcessorOptions? clientOptions = default)
    : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>(checkpointStore,
        eventBatchMaximumCount,
        consumerGroup,
        connectionString,
        eventHubName,
        clientOptions)
{
    public Func<IReadOnlyList<EventData>, Task> ProcessEventAsync { get; set; } = data => Task.CompletedTask;

    protected override async Task OnProcessingEventBatchAsync(IEnumerable<EventData> events,
        EventProcessorPartition partition,
        CancellationToken cancellationToken)
    {
        try
        {
            var readOnlyList = (IReadOnlyList<EventData>)events;
            if(readOnlyList.Count == 0)
            {
                return;
            }

            logger.BatchReceived(readOnlyList.Count, partition.PartitionId);

            await ProcessEventAsync(readOnlyList);

            var lastEvent = readOnlyList[^1];

            await UpdateCheckpointAsync(partition.PartitionId,
                new CheckpointPosition(lastEvent.OffsetString, lastEvent.SequenceNumber),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.BatchFailed(ex, partition.PartitionId);
        }
    }

    protected override Task OnProcessingErrorAsync(Exception exception,
        EventProcessorPartition partition,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        logger.ProcessingError(exception, partition.PartitionId, operationDescription);
        return Task.CompletedTask;
    }
}