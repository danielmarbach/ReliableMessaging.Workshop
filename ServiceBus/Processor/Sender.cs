using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Processor;

public class Sender : IHostedService
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;
    private readonly ILogger<Sender> logger;

    public Sender(IAzureClientFactory<ServiceBusClient> clientFactory, IOptions<ServiceBusOptions> serviceBusOptions, ILogger<Sender> logger)
    {
        serviceBusClient = clientFactory.CreateClient("Client");
        this.serviceBusOptions = serviceBusOptions;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var commandSender = serviceBusClient.CreateSender(serviceBusOptions.Value.InputQueue, new ServiceBusSenderOptions
        {
            Identifier = $"CommandSender-{serviceBusOptions.Value.InputQueue}"
        });

        var simulationCommands = CreateSimulationCommands();
        logger.LogInformation($"Sending {simulationCommands.Count} commands with duplicates {simulationCommands.Count - simulationCommands.DistinctBy(c => c.OrderId, StringComparer.Ordinal).Count()}");

        await foreach (var batch in Batches(simulationCommands, commandSender, cancellationToken))
        {
            using var batchToSend = batch;
            await commandSender.SendMessagesAsync(batchToSend, cancellationToken);
        }
    }

    private static Queue<SubmitOrder> CreateSimulationCommands()
    {
        var eventsToSend = new Queue<SubmitOrder>();
        for (var i = 0; i < 100; i++)
        {
            var submitOrder = new SubmitOrder
            {
                OrderId = $"orders/{Guid.NewGuid()}"
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

    static async IAsyncEnumerable<ServiceBusMessageBatch> Batches(Queue<SubmitOrder> queuedEvents,
        ServiceBusSender sender, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentBatch = default(ServiceBusMessageBatch);
        while (queuedEvents.Count > 0)
        {
            var command = queuedEvents.Peek();

            currentBatch ??= await sender.CreateMessageBatchAsync(cancellationToken);

            if (!currentBatch.TryAddMessage(new ServiceBusMessage(BinaryData.FromObjectAsJson(command))
                {
                    ContentType = "application/json",
                    MessageId = command.OrderId,
                    ApplicationProperties =
                    {
                        { "MessageType", typeof(SubmitOrder).FullName }
                    }
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
                queuedEvents.Dequeue();
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