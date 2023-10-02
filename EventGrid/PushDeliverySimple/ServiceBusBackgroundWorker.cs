using Azure.Messaging.ServiceBus;

namespace PushDelivery;

public class ServiceBusBackgroundWorker : BackgroundService
{
    private readonly Queue workQueue;
    private readonly ServiceBusClient serviceBusClient;
    private readonly ILoggerFactory loggerFactory;

    public ServiceBusBackgroundWorker(Queue workQueue, ServiceBusClient serviceBusClient, ILoggerFactory loggerFactory)
    {
        this.workQueue = workQueue;
        this.serviceBusClient = serviceBusClient;
        this.loggerFactory = loggerFactory;
    }

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
        // TODO
        // 1. Create a receiver for the queue using the Azure Service Bus SDK you are already familiar with
        // 1. Receive and process all messages from the queue by calling logger.HandleMessage
    }
}