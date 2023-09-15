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

    async Task Process(FetchMessages fetchMessages, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(fetchMessages.Queue);
        await using var processor = serviceBusClient.CreateProcessor(fetchMessages.Queue, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = Environment.ProcessorCount,
            AutoCompleteMessages = true
        });
        processor.ProcessMessageAsync += args =>
        {
            logger.LogInformation($"Handled messages with content '{args.Message.Body}'");
            return Task.CompletedTask;
        };
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Failure during processing");
            return Task.CompletedTask;
        };

        logger.LogInformation("Starting processing for 5 seconds");
        await processor.StartProcessingAsync(cancellationToken);

        await Task.Delay(5000, cancellationToken);

        logger.LogInformation("Stopping processing");
        await processor.StopProcessingAsync(cancellationToken);
    }
}

public record FetchMessages
{
    public FetchMessages(string queue)
    {
        Queue = queue;
    }

    public string Queue { get; init; }
}