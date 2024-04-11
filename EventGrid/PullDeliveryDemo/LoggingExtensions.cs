using Azure.Messaging.EventGrid.Namespaces;

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
    
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Locks {lockTokens} renewed. Succeeded {succeeded}. Failed {failed}")]
    public static partial void LocksRenewed(
        this ILogger logger, string[] lockTokens, IReadOnlyList<string> succeeded, IEnumerable<string> failed, [TagProvider(typeof(RenewCloudEventLocksResultTagProvider), nameof(RenewCloudEventLocksResultTagProvider.RecordTags))] RenewCloudEventLocksResult result);
    
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Lock renewal {lockTokens} stopped")]
    public static partial void LeaseRenewalStopped(this ILogger logger, string[] lockTokens);
}