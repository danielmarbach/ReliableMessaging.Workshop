using System.Text;
using Azure.Storage.Blobs;

namespace PullDeliveryDemo;

public class Sender(BlobContainerClient blobContainerClient, ILogger<Sender> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var blobName = $"{Guid.NewGuid().ToString()}.txt";
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes($"Hello, {DateTimeOffset.UtcNow}"));
            await blobContainerClient.UploadBlobAsync(blobName, memoryStream, stoppingToken);
            logger.BlobUploaded(blobName);
            await Task.Delay(3000, stoppingToken);
        }
    }
}