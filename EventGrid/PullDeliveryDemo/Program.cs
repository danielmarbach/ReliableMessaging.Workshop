using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using PullDeliveryDemo;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddBlobServiceClient(builder.Configuration.GetSection("StorageOptions")["ConnectionString"]);
    azureClientBuilder.AddEventGridSenderClient(new Uri(builder.Configuration.GetSection("EventGridOptions")["Endpoint"]), builder.Configuration.GetSection("EventGridOptions")["TopicName"], new AzureKeyCredential(builder.Configuration.GetSection("EventGridOptions")["AccessKey"]));
    azureClientBuilder.AddEventGridReceiverClient(new Uri(builder.Configuration.GetSection("EventGridOptions")["Endpoint"]), builder.Configuration.GetSection("EventGridOptions")["TopicName"], builder.Configuration.GetSection("EventGridOptions")["SubscriptionName"], new AzureKeyCredential(builder.Configuration.GetSection("EventGridOptions")["AccessKey"]));
});
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(nameof(StorageOptions)));
builder.Services.Configure<EventGridOptions>(builder.Configuration.GetSection(nameof(EventGridOptions)));
builder.Services.AddSingleton<BlobContainerClient>(provider =>
    provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(provider.GetRequiredService<IOptions<StorageOptions>>().Value.ContainerName));

builder.Services.AddHostedService<SetupInfrastructure>();
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<Receiver>();

var host = builder.Build();
await host.RunAsync();