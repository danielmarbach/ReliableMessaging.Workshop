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
            // TODO
            // 1. Create an envelope with an ActivateSensor command
            // 2. Set the corresponding headers
            // 3. Serialize the envelop using BinaryData
            // 4. Send the serialize envelop with a time to live of one day and no visibility timeout

            await Task.Delay(1000, stoppingToken);
        }
    }
}