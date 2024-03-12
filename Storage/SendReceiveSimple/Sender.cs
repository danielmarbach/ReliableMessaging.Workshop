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
            // Create an envelope with an ActivateSensor command
            // Set the corresponding headers
            // Serialize the envelop using BinaryData
            // Send the serialize envelop with a time to live of one day and no visibility timeout

            await Task.Delay(1000, stoppingToken);
        }
    }
}