using Azure.Messaging.EventHubs.Primitives;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Processor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddClient((StorageOptions options, IServiceProvider _) => new BlobServiceClient(options.ConnectionString));
    azureClientBuilder.AddClient((StorageOptions options, IServiceProvider provider) => provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(options.ContainerName));
    azureClientBuilder.AddEventHubProducerClient(builder.Configuration.GetSection("EventHubs")["ConnectionString"], builder.Configuration.GetSection("EventHubs")["Name"]);
});
builder.Services.Configure<SenderOptions>(builder.Configuration.GetSection(nameof(SenderOptions)));
builder.Services.Configure<ProcessorOptions>(builder.Configuration.GetSection(nameof(ProcessorOptions)));

builder.Services.AddSingleton<BlobCheckpointStore>(provider =>
    new BlobCheckpointStore(provider.GetRequiredService<BlobContainerClient>()));
builder.Services.AddSingleton<BatchProcessor>(provider =>
{
    var eventHubs = provider.GetRequiredService<IConfiguration>().GetSection("EventHubs");
    return new BatchProcessor(provider.GetRequiredService<BlobCheckpointStore>(), eventBatchMaximumCount: 100, "$Default",
        eventHubs["ConnectionString"]!, eventHubs["Names"]!, provider.GetRequiredService<ILogger<BatchProcessor>>());
});

builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<Processor.Processor>();

var host = builder.Build();
await host.RunAsync();