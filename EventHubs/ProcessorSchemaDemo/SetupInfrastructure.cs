using Azure;
using Azure.Storage.Blobs;

namespace ProcessorSchemaDemo;

public class SetupInfrastructure(BlobContainerClient blobContainerClient, ILogger<SetupInfrastructure> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        RequestFailedException? exception;
        do
        {
            try
            {
                // We need to create the blob container to make sure checkpoints can be written.
                await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                exception = null;
            }
            catch (RequestFailedException ex)
            {
                exception = ex;
                logger.LogDebug("Waiting for the container to be created.");
                await Task.Delay(500, cancellationToken);
            }
        } while (exception != null);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}