namespace PushDelivery;

static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Handled messages with content '{Body}'")]
    public static partial void HandledMessage(
        this ILogger logger, string body);
}