using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using PushDelivery;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAzureClients(azureClientBuilder =>
{
    azureClientBuilder.AddServiceBusClient(builder.Configuration.GetSection("ServiceBus")["ConnectionString"]);
});
builder.Services.AddSingleton<Queue>();
builder.Services.AddHostedService<ServiceBusBackgroundWorker>();
builder.Services.AddHttpClient();
var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options &&
        context.Request.Headers.TryGetValue("WebHook-Request-Callback", out var callbacks))
    {
        context.Response.StatusCode = 200;
        context.Response.Headers.Append("WebHook-Request-Origin", "eventgrid.azure.net");
        context.Response.Headers.Append("WebHook-Allowed-Rate", "120");

        var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient();

        context.Response.OnCompleted(static async state =>
        {
            var (httpClient, callbackUri) = (Tuple<HttpClient, string>) state;
            _ = await httpClient.GetStringAsync(callbackUri);
        }, Tuple.Create(httpClient, callbacks[0]));
    }
    else
    {
        await next();
    }
});

app.MapPost("/api/EventGridEventHandler", async ([FromBody] object request, Queue queue) =>
{
    // TODO
    // 1. Extract the cloud event data from the request
    // 2. Extract the ServiceBusActiveMessagesAvailableWithNoListenersEventData as a system event
    // 3. Throw a FetchMessagesFromQueue into local worker queue
    // Hints: look at CloudEvent and TryGetSystemEventData
});

app.Run();