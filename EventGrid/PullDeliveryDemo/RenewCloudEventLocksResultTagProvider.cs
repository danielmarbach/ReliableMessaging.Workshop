using Azure.Messaging.EventGrid.Namespaces;

namespace PullDeliveryDemo;

public static class RenewCloudEventLocksResultTagProvider
{
    public static void RecordTags(ITagCollector collector, RenewCloudEventLocksResult forecast)
    {
        for (var index = 0; index < forecast.SucceededLockTokens.Count; index++)
        {
            var succeededLockToken = forecast.SucceededLockTokens[index];
            collector.Add($"SucceededLockTokens[{index}]", succeededLockToken);
        }
        
        for (var index = 0; index < forecast.FailedLockTokens.Count; index++)
        {
            var failedLockToken = forecast.FailedLockTokens[index];
            collector.Add($"FailedLockTokens[{index}]{nameof(failedLockToken.LockToken)}", failedLockToken.LockToken);
            collector.Add($"FailedLockTokens[{index}]{nameof(failedLockToken.Error)}", failedLockToken.Error.Message);
        }
    }
}