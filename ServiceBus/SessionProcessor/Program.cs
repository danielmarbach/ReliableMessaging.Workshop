using Microsoft.Extensions.Azure;
using SessionProcessor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddServiceBusClient(builder.Configuration.GetSection("ServiceBusOptions")["ConnectionStringNoManageRights"]);
    azureClientBuilder.AddServiceBusAdministrationClient(builder.Configuration.GetSection("ServiceBusOptions")["ConnectionString"]);
});

builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(nameof(ServiceBusOptions)));

builder.Services.AddHostedService<SetupInfrastructure>();
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<InputQueueProcessor>();

var host = builder.Build();
await host.RunAsync();