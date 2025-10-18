using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Processor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddServiceBusClient(builder.Configuration.GetSection("ServiceBusOptions")["ConnectionStringNoManageRights"])
        .WithName("Client");
    azureClientBuilder.AddServiceBusClient(builder.Configuration.GetSection("ServiceBusOptions")["ConnectionStringNoManageRights"])
        .WithName("TransactionalClient")
        .ConfigureOptions(options =>
        {
           // TODO 17. Enable cross entity transactions
        });
    azureClientBuilder.AddServiceBusAdministrationClient(builder.Configuration.GetSection("ServiceBusOptions")["ConnectionString"]);
});
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(nameof(ServiceBusOptions)));

builder.Services.AddKeyedSingleton<ServiceBusClient>("Client", (provider, key) => provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>().CreateClient((string)key!));
builder.Services.AddKeyedSingleton<ServiceBusClient>("TransactionalClient", (provider, key) => provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>().CreateClient((string)key!));
builder.Services.AddHostedService<SetupInfrastructure>();
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<InputQueueProcessor>();
builder.Services.AddHostedService<DestinationProcessor>();

var host = builder.Build();
await host.RunAsync();