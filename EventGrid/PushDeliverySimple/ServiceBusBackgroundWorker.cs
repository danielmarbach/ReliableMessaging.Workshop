using Azure.Messaging.ServiceBus;

namespace PushDelivery;

public class ServiceBusBackgroundWorker(
    Queue workQueue,
    ServiceBusClient serviceBusClient,
    ILoggerFactory loggerFactory)
    : BackgroundService
{
    private readonly ServiceBusClient serviceBusClient = serviceBusClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var fetchMessages = await workQueue.Read(stoppingToken);
            foreach (var fetchMessage in fetchMessages)
            {
                _ = Process(fetchMessage, stoppingToken);
            }
        }
    }

    async Task Process(FetchMessagesFromQueue fetchMessages, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(fetchMessages.Queue);
        // TODO 4. Create a receiver for the queue using the Azure Service Bus SDK you are already familiar with
        // TODO 5. Receive and process all messages from the queue by calling logger.HandleMessage
    }
}