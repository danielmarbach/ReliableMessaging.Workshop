using Azure.Storage.Queues;

namespace SendReceive;

public class Sender(QueueServiceClient queueServiceClient, IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sender = queueServiceClient.GetQueueClient(configuration.GetSection("Storage")["QueueName"]);
        await sender.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        int messageNumber = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var envelop = new Envelop<ActivateSensor>()
            {
                Headers = new Dictionary<string, string>
                {
                    { "MessageType", typeof(ActivateSensor).FullName! }
                },
                Content = new ActivateSensor
                {
                    SomeValue = $"SomeValue{messageNumber++}"
                },
                Id = Guid.NewGuid()
            };

            var serializedEnvelop = BinaryData.FromObjectAsJson(envelop, StorageJsonContext.Default.Options);
            await sender.SendMessageAsync(serializedEnvelop, visibilityTimeout: TimeSpan.Zero,
                timeToLive: TimeSpan.FromDays(1), cancellationToken: stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}