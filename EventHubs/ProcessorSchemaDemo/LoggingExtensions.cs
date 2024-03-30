namespace ProcessorSchemaDemo;

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

    public static void LogTemperature(this ILogger<Processor> logger, int numberOfDataPointsObserved, string channel, TemperatureChanged temperatureChanged, ProcessorOptions processorOptions)
    {
        if (numberOfDataPointsObserved < processorOptions.NumberOfDataPointsToObserve)
        {
            if (numberOfDataPointsObserved == 0)
            {
                logger.BelowThresholdReset(channel, temperatureChanged.Current, temperatureChanged.Published, processorOptions.TemperatureThreshold);
            }
            else
            {
                logger.BelowThreshold(channel, temperatureChanged.Current, temperatureChanged.Published);
            }
        }
        else
        {
            logger.AboveThreshold(channel, temperatureChanged.Current, temperatureChanged.Published, processorOptions.TemperatureThreshold, numberOfDataPointsObserved);
        }
    }

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = " - {Channel}: {Current} / {Published} (below threshold of '{TemperatureThreshold}', reset)")]
    static partial void BelowThresholdReset(
        this ILogger<Processor> logger, string channel, double current, DateTimeOffset published, double temperatureThreshold);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = " - {Channel}: {Current} / {Published}")]
    static partial void BelowThreshold(
        this ILogger<Processor> logger, string channel, double current, DateTimeOffset published);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = " - {Channel}: {Current} / {Published} (above threshold of '{TemperatureThreshold}' for the last '{NumberOfDataPointsObserved}' data points)")]
    static partial void AboveThreshold(
        this ILogger<Processor> logger, string channel, double current, DateTimeOffset published, double temperatureThreshold, int numberOfDataPointsObserved);
}