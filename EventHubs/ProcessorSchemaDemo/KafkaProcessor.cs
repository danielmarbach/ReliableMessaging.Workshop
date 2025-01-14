using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
    private IConsumer<string, byte[]>? consumer;
    private string currentToken;

    [MemberNotNull(nameof(consumer))]
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = $"{eventHubsOptions.Value.FullyQualifiedNamespace}:9093",
            GroupId = "$Default",
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.OAuthBearer,
            SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc,
            SaslOauthbearerTokenEndpointUrl =
                $"https://login.microsoftonline.com/{Environment.GetEnvironmentVariable("AZURE_TENANT_ID")}/oauth2/v2.0/token",
            SaslOauthbearerClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
            SaslOauthbearerClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"),
            SaslOauthbearerScope = $"https://{eventHubsOptions.Value.FullyQualifiedNamespace}/.default",
            AutoOffsetReset = processorOptions.Value.RestartFromBeginning
                ? AutoOffsetReset.Earliest : AutoOffsetReset.Latest,
            //BrokerVersionFallback = "1.0.0",
            //Debug = "consumer,cgrp,fetch,protocol,broker,security",
        };

        consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(eventHubsOptions.Value.Name);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channelObservations = new ConcurrentDictionary<string, int>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = consumer!.Consume(stoppingToken);

            var channel = consumeResult.Message.Key;
            var contentType = Encoding.UTF8.GetString(consumeResult.Message.Headers.GetLastBytes("content-type"));

            var temperatureChanged = serializer.Deserialize<TemperatureChanged>(
                new MessageContent { Data = BinaryData.FromBytes(consumeResult.Message.Value), ContentType = contentType },
                stoppingToken);

            var numberOfDataPointsObserved = channelObservations.AddOrUpdate(channel,
                static (_, _) => 0,
                static (_, points, options) => options.CurrentTemperature > options.TemperatureThreshold ? points + 1 : 0,
                (CurrentTemperature: temperatureChanged.Current, TemperatureThreshold: processorOptions.Value.TemperatureThreshold));

            logger.LogTemperature(numberOfDataPointsObserved, channel, temperatureChanged, processorOptions.Value);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        consumer?.Close();
        consumer?.Dispose();

        await base.StopAsync(cancellationToken);
    }
}