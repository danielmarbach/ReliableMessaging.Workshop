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
        await EnsureContainerExists(blobContainerClient);

        var channelObservations = new ConcurrentDictionary<string, int>();
        batchProcessor.ProcessEventAsync += events =>
        {
            foreach (var @event in events)
            {
                var temperatureChanged = @event.EventBody.ToObjectFromJson<TemperatureChanged>();
                var channel = @event.Properties["Channel"].ToString()!;

                var numberOfDataPointsObserved = channelObservations.AddOrUpdate(channel,
                    static (_, _) => 0,
                    static (_, points, options) => options.CurrentTemperature > options.TemperatureThreshold ? points + 1 : 0,
                    (CurrentTemperature: temperatureChanged.Current, TemperatureThreshold: processorOptions.Value.TemperatureThreshold));

                logger.LogInformation(numberOfDataPointsObserved < processorOptions.Value.NumberOfDataPointsToObserve
                    ? $" - {channel}: {temperatureChanged.Current} / {temperatureChanged.Published}{(numberOfDataPointsObserved == 0 ? $" (below threshold of '{processorOptions.Value.TemperatureThreshold}', reset)" : string.Empty)}"
                    : $" - {channel}: {temperatureChanged.Current} / {temperatureChanged.Published} (above threshold of '{processorOptions.Value.TemperatureThreshold}' for the last '{numberOfDataPointsObserved}' data points)");
            }

            return Task.CompletedTask;
        };

        await batchProcessor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await batchProcessor.StopProcessingAsync(cancellationToken);
    }

    async Task EnsureContainerExists(BlobContainerClient containerClient)
    {
        RequestFailedException? exception;
        do
        {
            try
            {
                await containerClient.CreateIfNotExistsAsync();
                exception = null;
            }
            catch (RequestFailedException ex)
            {
                exception = ex;
                logger.LogDebug("Waiting for the container to be created.");
                await Task.Delay(500);
            }
        } while (exception != null);
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