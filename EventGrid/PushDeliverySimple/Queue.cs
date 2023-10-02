using System.Threading.Channels;

namespace PushDelivery;

public class Queue
{
    private readonly Channel<FetchMessagesFromQueue> channel = Channel.CreateUnbounded<FetchMessagesFromQueue>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = true
    });

    public async ValueTask Enqueue(FetchMessagesFromQueue fetchMessages)
    {
        await channel.Writer.WriteAsync(fetchMessages);
    }

    public async ValueTask<IReadOnlyCollection<FetchMessagesFromQueue>> Read(CancellationToken cancellationToken)
    {
        if (!await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            return Array.Empty<FetchMessagesFromQueue>();
        }

        var hashSet = new HashSet<FetchMessagesFromQueue>(EqualityComparer<FetchMessagesFromQueue>.Default);
        while (channel.Reader.TryRead(out var fetchMessage))
        {
            hashSet.Add(fetchMessage);
        }
        return hashSet;
    }
}