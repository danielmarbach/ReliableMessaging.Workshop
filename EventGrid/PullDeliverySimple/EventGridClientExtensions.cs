using System.Runtime.CompilerServices;
using Azure.Messaging.EventGrid.Namespaces;

namespace PullDelivery;

public static class EventGridClientExtensions
{
    public static async IAsyncEnumerable<ReceiveDetails> StreamCloudEvents(this EventGridReceiverClient client,
        int? maxEvents = null, TimeSpan? maxWaitTime = null, [EnumeratorCancellation] CancellationToken token = default)
    {
        var receivedEvents = 0;
        do
        {
            var receivedMessages = await client.ReceiveAsync(maxEvents: Math.Min(100, maxEvents ?? 100), maxWaitTime: maxWaitTime, token);
            foreach (var receiveDetails in receivedMessages?.Value.Details ?? Enumerable.Empty<ReceiveDetails>())
            {
                receivedEvents++;
                yield return receiveDetails;
            }
        } while (receivedEvents < maxEvents && !token.IsCancellationRequested);
    }
}