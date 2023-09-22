// See https://aka.ms/new-console-template for more information

using Azure;
using Microsoft.Extensions.Azure;
using PullDelivery;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddEventGridClient(new Uri(builder.Configuration.GetSection("EventGridOptions")["Endpoint"]), new AzureKeyCredential(builder.Configuration.GetSection("EventGridOptions")["AccessKey"]));
});
builder.Services.Configure<EventGridOptions>(builder.Configuration.GetSection(nameof(EventGridOptions)));
builder.Services.AddHostedService<Sender>();
builder.Services.AddHostedService<Receiver>();

var host = builder.Build();
await host.RunAsync();