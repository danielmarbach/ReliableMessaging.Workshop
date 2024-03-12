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
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RestartFromBeginningIfNecessary(processorOptions.Value.RestartFromBeginning, blobContainerClient);

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

                logger.LogTemperature(numberOfDataPointsObserved, channel, temperatureChanged, processorOptions.Value);
            }

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