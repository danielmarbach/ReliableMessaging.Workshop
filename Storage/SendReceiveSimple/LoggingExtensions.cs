namespace SendReceive;

static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Start handling message '{MessageId}' with '{SomeValue}'")]
    public static partial void StartHandling(this ILogger logger, string messageId, string someValue);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Done handling message '{MessageId}' with '{SomeValue}'")]
    public static partial void DoneHandling(this ILogger logger, string messageId, string someValue);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Lease renewed {MessageId} original '{PopReceipt}' and new '{UpdatePopReceipt}'")]
    public static partial void LeaseRenewed(this ILogger logger, string messageId, string popReceipt, string updatePopReceipt);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Lease renewal {MessageId} stopped")]
    public static partial void LeaseRenewalStopped(this ILogger logger, string messageId);
}