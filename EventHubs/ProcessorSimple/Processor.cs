using System.Collections.Concurrent;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace Processor;

public class Processor(
    IOptions<ProcessorOptions> processorOptions,
    BlobContainerClient blobContainerClient,
    BatchProcessor batchProcessor,
    ILogger<Processor> logger)
    : IHostedService
{
    private readonly ILogger<Processor> logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RestartFromBeginningIfNecessary(processorOptions.Value.RestartFromBeginning, blobContainerClient);

        batchProcessor.ProcessEventAsync += events =>
        {
            // TODO 4. Process the batch of events by implementing the following algorithm:
            // For every channel increase the number of data points observed by one if the temperature is above the threshold
            // when the temperature is below the threshold reset the number of data points observed back to zero
            // log something at information level when the data is below or above the threshold
            return Task.CompletedTask;
        };

        await batchProcessor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await batchProcessor.StopProcessingAsync(cancellationToken);
    }

    static async Task RestartFromBeginningIfNecessary(bool restartFromBeginning, BlobContainerClient containerClient)
    {
        if (restartFromBeginning)
        {
            try
            {
                await containerClient.DeleteAsync();
            }
            catch (RequestFailedException ex) when(ex.ErrorCode == "ContainerNotFound")
            {
            }
        }
    }
}