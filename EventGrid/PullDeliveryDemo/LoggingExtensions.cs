namespace PullDeliveryDemo;

static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Blob {Name} uploaded.")]
    public static partial void BlobUploaded(
        this ILogger logger, string name);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Blob {Name} with content {Content} downloaded.")]
    public static partial void BlobDownloaded(
        this ILogger logger, string name, string content);
}