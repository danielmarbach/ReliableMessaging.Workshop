using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging;
using Confluent.Kafka;
using Microsoft.Azure.Data.SchemaRegistry.ApacheAvro;
using Microsoft.Extensions.Options;

namespace ProcessorSchemaDemo;

public class KafkaProcessor(
    IOptions<ProcessorOptions> processorOptions,
    IOptions<EventHubsOptions> eventHubsOptions,
    SchemaRegistryAvroSerializer serializer,
    ILogger<KafkaProcessor> logger)
    : BackgroundService
{
    private IConsumer<string, byte[]>? _consumer;

    [MemberNotNull(nameof(_consumer))]
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = eventHubsOptions.Value.FullyQualifiedNamespace,
            GroupId = "$Default",
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SaslOauthbearerTokenEndpointUrl = "https://login.microsoftonline.com/" +
                                              Environment.GetEnvironmentVariable("AZURE_TENANT_ID") + "/oauth2/v2.0/token",
            SaslOauthbearerClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
            SaslOauthbearerClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"),
            SaslOauthbearerScope = "https://eventgrid.azure.net/.default",
            AutoOffsetReset = processorOptions.Value.RestartFromBeginning ?
                AutoOffsetReset.Earliest : AutoOffsetReset.Latest
        };

        _consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        _consumer.Subscribe(eventHubsOptions.Value.Name);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var channelObservations = new ConcurrentDictionary<string, int>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = _consumer!.Consume(stoppingToken);

            var temperatureChanged = serializer.Deserialize<TemperatureChanged>(
                new MessageContent { Data = BinaryData.FromBytes(consumeResult.Message.Value) },
                stoppingToken);

            var channel = consumeResult.Message.Headers
                .GetLastBytes("Channel")
                ?.ToString() ?? "default";

            var numberOfDataPointsObserved = channelObservations.AddOrUpdate(channel,
                static (_, _) => 0,
                static (_, points, options) => options.CurrentTemperature > options.TemperatureThreshold ? points + 1 : 0,
                (CurrentTemperature: temperatureChanged.Current, TemperatureThreshold: processorOptions.Value.TemperatureThreshold));

            logger.LogTemperature(numberOfDataPointsObserved, channel, temperatureChanged, processorOptions.Value);

            _consumer.StoreOffset(consumeResult);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Close();
        _consumer?.Dispose();

        await base.StopAsync(cancellationToken);
    }
} 