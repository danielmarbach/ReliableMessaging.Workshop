namespace SessionProcessor;

static partial class LoggingExtensions
{
    public static void LogTemperature(this ILogger<InputQueueProcessor> logger, int numberOfDataPointsObserved, string channel, ProcessTemperatureChange temperatureChanged, ServiceBusOptions serviceBusOptions)
    {
        if (numberOfDataPointsObserved < serviceBusOptions.NumberOfDataPointsToObserve)
        {
            if (numberOfDataPointsObserved == 0)
            {
                logger.BelowThresholdReset(channel, temperatureChanged.Current, temperatureChanged.Published, serviceBusOptions.TemperatureThreshold);
            }
            else
            {
                logger.BelowThreshold(channel, temperatureChanged.Current, temperatureChanged.Published);
            }
        }
        else
        {
            logger.AboveThreshold(channel, temperatureChanged.Current, temperatureChanged.Published, serviceBusOptions.TemperatureThreshold, numberOfDataPointsObserved);
        }
    }

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = " - {Channel}: {Current} / {Published} (below threshold of '{TemperatureThreshold}', reset)")]
    static partial void BelowThresholdReset(
        this ILogger<InputQueueProcessor> logger, string channel, double current, DateTimeOffset published, double temperatureThreshold);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = " - {Channel}: {Current} / {Published}")]
    static partial void BelowThreshold(
        this ILogger<InputQueueProcessor> logger, string channel, double current, DateTimeOffset published);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = " - {Channel}: {Current} / {Published} (above threshold of '{TemperatureThreshold}' for the last '{NumberOfDataPointsObserved}' data points)")]
    static partial void AboveThreshold(
        this ILogger<InputQueueProcessor> logger, string channel, double current, DateTimeOffset published, double temperatureThreshold, int numberOfDataPointsObserved);
}