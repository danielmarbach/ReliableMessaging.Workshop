using Azure.Identity;
using Azure.Messaging.ServiceBus;

var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
var fullyQualifiedNamespace = Environment.GetEnvironmentVariable("SERVICE_BUS_FULLY_QUALIFIED_NAMESPACE");

var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
var client = new ServiceBusClient(fullyQualifiedNamespace, credential);
var sender = client.CreateSender("rbacqueue");
Console.WriteLine("Sending message...");
await sender.SendMessageAsync(new ServiceBusMessage());
Console.WriteLine("Message sent.");

var receiver = client.CreateReceiver("rbacqueue");
Console.WriteLine("Receiving message(s)...");
// will fail because of missing Listen permission
await foreach (var message in receiver.ReceiveMessagesAsync())
{
}