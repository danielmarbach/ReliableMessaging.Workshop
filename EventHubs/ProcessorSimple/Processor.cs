using System.Collections.Concurrent;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace Processor;

public class Processor : IHostedService
{
    private readonly IOptions<ProcessorOptions> processorOptions;
    private readonly BlobContainerClient blobContainerClient;
    private readonly BatchProcessor batchProcessor;
    private readonly ILogger<Processor> logger;

    public Processor(IOptions<ProcessorOptions> processorOptions, BlobContainerClient blobContainerClient, BatchProcessor batchProcessor, ILogger<Processor> logger)
    {
        this.processorOptions = processorOptions;
        this.blobContainerClient = blobContainerClient;
        this.batchProcessor = batchProcessor;
        this.logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RestartFromBeginningIfNecessary(processorOptions.Value.RestartFromBeginning, blobContainerClient);

        var channelObservations = new ConcurrentDictionary<string, int>();
        batchProcessor.ProcessEventAsync += events =>
        {
            // TODO
            // Process the batch of events by implementing the following algorithm:
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