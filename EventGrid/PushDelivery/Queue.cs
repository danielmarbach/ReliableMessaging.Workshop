using System.Threading.Channels;

namespace PushDelivery;

public class Queue
{
    private readonly Channel<FetchMessages> channel;

    public Queue()
    {
        channel = Channel.CreateUnbounded<FetchMessages>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        });
    }

    public async ValueTask Enqueue(FetchMessages fetchMessages)
    {
        await channel.Writer.WriteAsync(fetchMessages);
    }

    public async ValueTask<IReadOnlyCollection<FetchMessages>> Read(CancellationToken cancellationToken)
    {
        if (!await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            return Array.Empty<FetchMessages>();
        }

        var hashSet = new HashSet<FetchMessages>(EqualityComparer<FetchMessages>.Default);
        while (channel.Reader.TryRead(out var fetchMessage))
        {
            hashSet.Add(fetchMessage);
        }
        return hashSet;
    }
}