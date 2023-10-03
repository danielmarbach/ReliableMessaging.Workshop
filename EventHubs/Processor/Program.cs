using Azure.Messaging.EventHubs.Primitives;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Processor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddBlobServiceClient(builder.Configuration.GetSection("StorageOptions")["ConnectionString"]);
    azureClientBuilder.AddEventHubProducerClient(builder.Configuration.GetSection("EventHubs")["ConnectionString"], builder.Configuration.GetSection("EventHubs")["Name"]);
});
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(nameof(StorageOptions)));
builder.Services.Configure<SenderOptions>(builder.Configuration.GetSection(nameof(SenderOptions)));
builder.Services.Configure<ProcessorOptions>(builder.Configuration.GetSection(nameof(ProcessorOptions)));

builder.Services.AddSingleton<BlobContainerClient>(provider =>
    provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(provider.GetRequiredService<IOptions<StorageOptions>>().Value.ContainerName));
builder.Services.AddSingleton<BlobCheckpointStore>(provider =>
    new BlobCheckpointStore(provider.GetRequiredService<BlobContainerClient>()));
builder.Services.AddSingleton<BatchProcessor>(provider =>
{
    var eventHubs = provider.GetRequiredService<IConfiguration>().GetSection("EventHubs");
    return new BatchProcessor(provider.GetRequiredService<BlobCheckpointStore>(), eventBatchMaximumCount: 100, "$Default",
        eventHubs["ConnectionString"]!, eventHubs["Name"]!, provider.GetRequiredService<ILogger<BatchProcessor>>());
});

builder.Services.AddHostedService<SetupInfrastructure>();
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<Processor.Processor>();

var host = builder.Build();
await host.RunAsync();