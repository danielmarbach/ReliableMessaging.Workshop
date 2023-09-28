using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace SessionProcessor;

public class InputQueueProcessor : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;
    private readonly ILogger<Sender> logger;
    private ServiceBusSessionProcessor? queueProcessor;

    public InputQueueProcessor(IAzureClientFactory<ServiceBusClient> clientFactory, IOptions<ServiceBusOptions> serviceBusOptions, ILogger<Sender> logger)
    {
        serviceBusClient = clientFactory.CreateClient("Client");
        this.serviceBusOptions = serviceBusOptions;
        this.logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        queueProcessor = serviceBusClient.CreateSessionProcessor(serviceBusOptions.Value.InputQueue, new ServiceBusSessionProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentSessions = 10,
            AutoCompleteMessages = true,
            PrefetchCount = 10,
            Identifier = $"SessionProcessor-{serviceBusOptions.Value.InputQueue}",
            MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(10),
        });
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
        var message = arg.Message;
        var channel = arg.SessionId;
        var processTemperatureChange = message.Body.ToObjectFromJson<ProcessTemperatureChange>();

        var sessionState = await arg.GetSessionStateAsync(cancellationToken);
        var channelState = sessionState?.ToObjectFromJson<ChannelState>() ?? new ChannelState();

        channelState.PointsObserved +=
            processTemperatureChange.Current > serviceBusOptions.Value.TemperatureThreshold
            ? 1 : -channelState.PointsObserved;

        logger.LogInformation(channelState.PointsObserved < serviceBusOptions.Value.NumberOfDataPointsToObserve
            ? $" - {channel}: {processTemperatureChange.Current} / {processTemperatureChange.Published}{(channelState.PointsObserved == 0 ? $" (below threshold of '{serviceBusOptions.Value.TemperatureThreshold}', reset)" : string.Empty)}"
            : $" - {channel}: {processTemperatureChange.Current} / {processTemperatureChange.Published} (above threshold of '{serviceBusOptions.Value.TemperatureThreshold}' for the last '{channelState.PointsObserved}' data points)");

        await arg.SetSessionStateAsync(BinaryData.FromObjectAsJson(channelState), cancellationToken);
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