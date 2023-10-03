using Azure;
using Azure.Storage.Blobs;

namespace Processor;

public class SetupInfrastructure : IHostedService
{
    private readonly BlobContainerClient blobContainerClient;
    private readonly ILogger<SetupInfrastructure> logger;

    public SetupInfrastructure(BlobContainerClient blobContainerClient, ILogger<SetupInfrastructure> logger)
    {
        this.blobContainerClient = blobContainerClient;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        RequestFailedException? exception;
        do
        {
            try
            {
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