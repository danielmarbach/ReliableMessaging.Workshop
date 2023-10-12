using Microsoft.Extensions.Azure;
using SendReceive;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddQueueServiceClient(builder.Configuration.GetSection("Storage")["ConnectionString"]);
});
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<Receiver>();

var host = builder.Build();
await host.RunAsync();