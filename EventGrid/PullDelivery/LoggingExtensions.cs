namespace PullDelivery;

static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = " - {Current} / {Published}")]
    public static partial void TemperatureChanged(
        this ILogger logger, double current, DateTimeOffset published);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Releasing {Count}")]
    public static partial void Releasing(
        this ILogger logger, int count);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Acknowledge {Count}")]
    public static partial void Acknowledge(
        this ILogger logger, int count);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Reject {Count}")]
    public static partial void Reject(
        this ILogger logger, int count);
}