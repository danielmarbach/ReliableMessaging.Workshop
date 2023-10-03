namespace Processor;

static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Received batch of {ItemsCount} event on partition '{PartitionId}'")]
    public static partial void BatchReceived(
        this ILogger<BatchProcessor> logger, int itemsCount, string partitionId);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Error processing partition '{PartitionId}'")]
    public static partial void BatchFailed(
        this ILogger<BatchProcessor> logger, Exception exception, string partitionId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Error processing partition '{PartitionId}' with '{OperationDescription}'")]
    public static partial void ProcessingError(
        this ILogger<BatchProcessor> logger, Exception exception, string partitionId, string operationDescription);

}