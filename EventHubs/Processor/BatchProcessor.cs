using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;

namespace Processor;

public sealed class BatchProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>
{
    private readonly ILogger<BatchProcessor> logger;

    public BatchProcessor(CheckpointStore checkpointStore,
        int eventBatchMaximumCount,
        string consumerGroup,
        string connectionString,
        string eventHubName,
        ILogger<BatchProcessor> logger,
        EventProcessorOptions? clientOptions = default)
        : base(
            checkpointStore,
            eventBatchMaximumCount,
            consumerGroup,
            connectionString,
            eventHubName,
            clientOptions)
    {
        this.logger = logger;
    }

    public Func<IReadOnlyList<EventData>, Task> ProcessEventAsync { get; set; } = data => Task.CompletedTask;

    protected override async Task OnProcessingEventBatchAsync(IEnumerable<EventData> events,
        EventProcessorPartition partition,
        CancellationToken cancellationToken)
    {
        try
        {
            var readOnlyList = (IReadOnlyList<EventData>)events;

            logger.LogDebug($"Received batch of {readOnlyList.Count} event on partition {partition.PartitionId}");

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
            logger.LogError($"Error processing partition '{partition.PartitionId} due to {ex.Message}");
        }
    }

    protected override Task OnProcessingErrorAsync(Exception exception,
        EventProcessorPartition partition,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, $"Error processing partition '{partition.PartitionId} with '{operationDescription}' due to {exception.Message}");
        return Task.CompletedTask;
    }
}