using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace Processor;

public class Sender(
    [FromKeyedServices("Client")] ServiceBusClient serviceBusClient,
    IOptions<ServiceBusOptions> serviceBusOptions,
    ILogger<Sender> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var commandSender = serviceBusClient.CreateSender(serviceBusOptions.Value.InputQueue, new ServiceBusSenderOptions
        {
            Identifier = $"CommandSender-{serviceBusOptions.Value.InputQueue}"
        });

        var simulationCommands = CreateSimulationCommands();
        logger.SendWithDuplicates(simulationCommands.Count, simulationCommands.Count - simulationCommands.DistinctBy(c => c.ChannelId, StringComparer.Ordinal).Count());

        await foreach (var batch in Batches(simulationCommands, commandSender, cancellationToken))
        {
            // TODO 5. Send a batch
        }
    }

    private Queue<ActivateSensor> CreateSimulationCommands()
    {
        var eventsToSend = new Queue<ActivateSensor>();
        for (var i = 0; i < serviceBusOptions.Value.NumberOfCommands; i++)
        {
            var submitOrder = new ActivateSensor
            {
                ChannelId = $"channels/{Guid.NewGuid()}"
            };
            eventsToSend.Enqueue(submitOrder);

            // create some duplicates
            if (i % 3 == 0)
            {
                eventsToSend.Enqueue(submitOrder);
            }
        }

        return eventsToSend;
    }

    static async IAsyncEnumerable<ServiceBusMessageBatch> Batches(Queue<ActivateSensor> queueCommands,
        ServiceBusSender sender, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentBatch = default(ServiceBusMessageBatch);
        while (queueCommands.Count > 0)
        {
            var command = queueCommands.Peek();

            // TODO 1. Create a batch when currentBatch is null
            // TODO 2. Try adding a a new ServiceBusMessage with the command as json content
            // TODO 3. Use the channel id as message ID to make sure messages can be de-duplicated
            // TODO 4. Use the application properties to set the "MessageType" to the full name of the payload
            if (false)
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