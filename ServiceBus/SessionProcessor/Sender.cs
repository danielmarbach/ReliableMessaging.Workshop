using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace SessionProcessor;

public class Sender(
    IAzureClientFactory<ServiceBusClient> clientFactory,
    IOptions<ServiceBusOptions> serviceBusOptions,
    ILogger<Sender> logger)
    : IHostedService
{
    private readonly ServiceBusClient serviceBusClient = clientFactory.CreateClient("Client");
    private readonly ILogger<Sender> logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var commandSender = serviceBusClient.CreateSender(serviceBusOptions.Value.InputQueue, new ServiceBusSenderOptions
        {
            Identifier = $"CommandSender-{serviceBusOptions.Value.InputQueue}"
        });

        foreach (var channel in serviceBusOptions.Value.Channels)
        {
            var simulationCommands = CreateSimulationCommands();
            await foreach (var batch in Batches(channel, simulationCommands, commandSender, cancellationToken))
            {
                using var batchToSend = batch;
                await commandSender.SendMessagesAsync(batchToSend, cancellationToken);
            }
        }
    }

    private Queue<ProcessTemperatureChange> CreateSimulationCommands()
    {
        var eventsToSend = new Queue<ProcessTemperatureChange>();
        var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2));
        for (var i = 0; i < serviceBusOptions.Value.NumberOfCommandsPerChannel; i++)
        {
            var processTemperatureChange = new ProcessTemperatureChange
            {
                Published = yesterday.Add(TimeSpan.FromSeconds(i)),
                Current = Random.Shared.Next(20, 30) + Random.Shared.NextDouble()
            };
            eventsToSend.Enqueue(processTemperatureChange);
        }

        return eventsToSend;
    }

    static async IAsyncEnumerable<ServiceBusMessageBatch> Batches(string channel, Queue<ProcessTemperatureChange> queueCommands,
        ServiceBusSender sender, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentBatch = default(ServiceBusMessageBatch);
        while (queueCommands.Count > 0)
        {
            var command = queueCommands.Peek();

            currentBatch ??= await sender.CreateMessageBatchAsync(cancellationToken);

            if (!currentBatch.TryAddMessage(new ServiceBusMessage(BinaryData.FromObjectAsJson(command))
                {
                    ContentType = "application/json",
                    ApplicationProperties =
                    {
                        { "MessageType", typeof(ProcessTemperatureChange).FullName }
                    },
                    SessionId = channel
                }))
            {
                if (currentBatch.Count == 0)
                {
                    throw new Exception("There was an event too large to fit into a batch.");
                }

                yield return currentBatch;
                currentBatch = default;
            }
            else
            {
                queueCommands.Dequeue();
            }
        }

        if (currentBatch is { Count: > 0 })
        {
            yield return currentBatch;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}