using Azure.Data.SchemaRegistry;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Storage.Blobs;
using Microsoft.Azure.Data.SchemaRegistry.ApacheAvro;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using ProcessorSchemaDemo;

Console.WriteLine(Environment.GetEnvironmentVariable("AZURE_TENANT_ID"));

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddBlobServiceClient(new Uri(builder.Configuration.GetSection("StorageOptions")["ServiceUri"]));
    azureClientBuilder.AddEventHubProducerClientWithNamespace(builder.Configuration.GetSection("EventHubs")["FullyQualifiedNamespace"], builder.Configuration.GetSection("EventHubs")["Name"]);
    azureClientBuilder.AddSchemaRegistryClient(builder.Configuration.GetSection("EventHubs")["FullyQualifiedNamespace"]);
});
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(nameof(StorageOptions)));
builder.Services.Configure<SenderOptions>(builder.Configuration.GetSection(nameof(SenderOptions)));
builder.Services.Configure<ProcessorOptions>(builder.Configuration.GetSection(nameof(ProcessorOptions)));
builder.Services.Configure<SerializerOptions>(builder.Configuration.GetSection(nameof(SerializerOptions)));
builder.Services.Configure<EventHubsOptions>(builder.Configuration.GetSection("EventHubs"));

builder.Services.AddSingleton<BlobContainerClient>(provider =>
    provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(provider.GetRequiredService<IOptions<StorageOptions>>().Value.ContainerName));
builder.Services.AddSingleton<BlobCheckpointStore>(provider =>
    new BlobCheckpointStore(provider.GetRequiredService<BlobContainerClient>()));
builder.Services.AddSingleton<BatchProcessor>(provider =>
{
    var eventHubs = provider.GetRequiredService<IConfiguration>().GetSection("EventHubs");
    return new BatchProcessor(provider.GetRequiredService<BlobCheckpointStore>(), eventBatchMaximumCount: 100, "$Default",
        eventHubs["FullyQualifiedNamespace"]!, eventHubs["Name"]!, provider.GetRequiredService<ILogger<BatchProcessor>>());
});
builder.Services.AddSingleton<SchemaRegistryAvroSerializer>(provider => new SchemaRegistryAvroSerializer(provider.GetRequiredService<SchemaRegistryClient>(), provider.GetRequiredService<IOptions<SerializerOptions>>().Value.SchemaGroup));

builder.Services.AddHostedService<SetupInfrastructure>();
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<Processor>();
//builder.Services.AddHostedService<KafkaProcessor>();

var host = builder.Build();
await host.RunAsync();