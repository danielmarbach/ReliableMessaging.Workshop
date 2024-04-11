using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using PullDeliveryDemo;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddBlobServiceClient(builder.Configuration.GetSection("StorageOptions")["ConnectionString"]);
    // This is a bit of a quirk in the beta builds :(
    EventGridClientBuilderExtensions.AddEventGridClient(azureClientBuilder, new Uri(builder.Configuration.GetSection("EventGridOptions")["Endpoint"]), new AzureKeyCredential(builder.Configuration.GetSection("EventGridOptions")["AccessKey"]));
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