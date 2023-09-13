using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.EventGrid.Namespaces;

namespace PullDelivery;

public static class EventGridClientExtensions
{
    public static async IAsyncEnumerable<ReceiveDetails> StreamCloudEvents(this EventGridClient client, string topicName, string eventSubscriptionName,
        int? maxEvents = null, TimeSpan? maxWaitTime = null, [EnumeratorCancellation] CancellationToken token = default)
    {
        var receivedEvents = 0;
        do
        {
            Response<ReceiveResult>? receivedMessages = await client
                .ReceiveCloudEventsAsync(topicName, eventSubscriptionName, maxEvents: Math.Min(100, maxEvents ?? 100), maxWaitTime, token);
            foreach (var receiveDetails in receivedMessages?.Value.Value ?? Enumerable.Empty<ReceiveDetails>())
            {
                receivedEvents++;
                yield return receiveDetails;
            }
        } while (receivedEvents < maxEvents && !token.IsCancellationRequested);
    }
}