using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace SessionProcessor;

public class InputQueueProcessor : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;
    private readonly ILogger<InputQueueProcessor> logger;
    private ServiceBusSessionProcessor? queueProcessor;

    public InputQueueProcessor(IAzureClientFactory<ServiceBusClient> clientFactory, IOptions<ServiceBusOptions> serviceBusOptions, ILogger<InputQueueProcessor> logger)
    {
        serviceBusClient = clientFactory.CreateClient("Client");
        this.serviceBusOptions = serviceBusOptions;
        this.logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // TODO
        // Create a session processor that auto completes messages, has maximum 10 concurrent sessions and a prefetch count of 10
        queueProcessor = null;
        queueProcessor.ProcessMessageAsync += ProcessMessages;
        queueProcessor.ProcessErrorAsync += ProcessError;
        await queueProcessor.StartProcessingAsync(cancellationToken);
    }

    private Task ProcessError(ProcessErrorEventArgs arg)
    {
        if (arg.Exception is OperationCanceledException)
        {
            return Task.CompletedTask;
        }
        logger.LogError(arg.Exception, "Error processing message");
        return Task.CompletedTask;
    }

    private async Task ProcessMessages(ProcessSessionMessageEventArgs arg)
    {
        arg.Message.ApplicationProperties.TryGetValue("MessageType", out var messageTypeValue);
        var handlerTask = messageTypeValue switch
        {
            "SessionProcessor.ProcessTemperatureChange" => HandleProcessTemperatureChange(arg, arg.CancellationToken),
            _ => Task.CompletedTask
        };
        await handlerTask;
    }

    record ChannelState
    {
        public int PointsObserved { get; set; } = 0;
    }

    async Task HandleProcessTemperatureChange(ProcessSessionMessageEventArgs arg, CancellationToken cancellationToken)
    {
        // TODO
        // Deserialize the ProcessTemperatureChange command from the message
        // Use ChannelState as a session state to durably store the last data points observed
        // Implement the following algorithm:
        // For every channel increase the number of data points observed by one if the temperature is above the threshold
        // when the temperature is below the threshold reset the number of data points observed back to zero
        // log something at information level when the data is below or above the threshold
        var message = arg.Message;
        var channel = arg.SessionId;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (queueProcessor is not null)
        {
            await queueProcessor.StopProcessingAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (queueProcessor is not null)
        {
            await queueProcessor.DisposeAsync();
        }
    }
}