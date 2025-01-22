using Azure;
using Microsoft.Extensions.Azure;
using PullDelivery;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddEventGridSenderClient(new Uri(builder.Configuration.GetSection("EventGridOptions")["Endpoint"]), builder.Configuration.GetSection("EventGridOptions")["TopicName"], new AzureKeyCredential(builder.Configuration.GetSection("EventGridOptions")["AccessKey"]));
    azureClientBuilder.AddEventGridReceiverClient(new Uri(builder.Configuration.GetSection("EventGridOptions")["Endpoint"]), builder.Configuration.GetSection("EventGridOptions")["TopicName"], builder.Configuration.GetSection("EventGridOptions")["SubscriptionName"], new AzureKeyCredential(builder.Configuration.GetSection("EventGridOptions")["AccessKey"]));
});
builder.Services.Configure<EventGridOptions>(builder.Configuration.GetSection(nameof(EventGridOptions)));
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<Receiver>();

var host = builder.Build();
await host.RunAsync();