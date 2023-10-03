using Azure.Storage.Blobs;

namespace Processor;

public class SetupInfrastructure : IHostedService
{
    private readonly BlobContainerClient blobContainerClient;

    public SetupInfrastructure(BlobContainerClient blobContainerClient)
    {
        this.blobContainerClient = blobContainerClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}