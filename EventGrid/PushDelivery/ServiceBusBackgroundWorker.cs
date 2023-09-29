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
        await using var receiver = serviceBusClient.CreateReceiver(fetchMessages.Queue, new ServiceBusReceiverOptions
        {
            // for the demo
            PrefetchCount = 100,
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        });
        logger.LogInformation("Starting processing");

        var messages = await receiver.ReceiveMessagesAsync(100, cancellationToken: cancellationToken);
        foreach (var message in messages)
        {
            logger.HandledMessage(message.Body.ToString());
        }

        logger.LogInformation("Stopping processing");
    }
}