# ReliableMessaging Workshop

Azure offers multiple services to achieve durable and reliable messaging. But, which one (or ones) do you need and why? What features does each one offer that make them stand out and where do they fall short? What about cost? In this two-day workshop, you will learn the fundamentals of reliable messaging using a range of Azure services and get hands-on experience coding against them. Examples will be in C# .NET.

First, we start with Azure Queue Storage-a basic, cost-effective, and durable message queue that enables effective decoupling of components, increases resilience, and supports scalability scenarios.

After getting our feet wet with this no-frills queuing technology, we dive deep into feature-rich Azure Service Bus. This service is replete with all the bells and whistles needed to build robust and reliable applications leveraging the publish/subscribe pattern.

We also take a peek into Azure Event Hubs, which supports streaming millions of events per second to build real-time data ingestion services.

We end the day by learning how to leverage the power of Azure Event Grid to combine various messaging services in order to take advantage of their potential without the need for polling.

After this workshop, you will be armed with a solid foundational understanding of Azure messaging service offerings and will be able to identify which one is appropriate for your needs. Furthermore, you will walk away with the benefits of practical experience coding against those services, jump-starting your career building reliable messaging solutions in Azure.

This is a Bring Your Own Device (BYOD) course. At BYOD courses, attendees are required to bring their own laptop with the necessary software already installed. To use Azure messaging services, you'll need a subscription (*). If you do not have an existing Azure account, you may sign up for a free trial (https://azure.microsoft.com/free/dotnet/) or use your Visual Studio Subscription (https://visualstudio.microsoft.com/subscriptions/) benefits when you create an account (https://azure.microsoft.com/account).

Required:

- .NET 8
- Visual Studio/Rider/VS Code
- VS Code Extension for Bicep

Optional:

- Azure Service Bus Explorer
- Azure Storage Explorer
- Free ngrok account with a custom domain or a devtunnels account

(*) Some Azure resources may be created and provided to attendees with connection strings on a limited basis.

## Time table (One Day Version)

| 09:00 - 10:30 | Storage Queues |
|---|---|
| 10:30 - 10:50 | break |
| 10:50 - 12:30 | Service Bus |
| 12:30 - 13:30 | Lunch |
| 13:30 - 14:00 | Service Bus |
| 14:00 - 15:00 | Event Hubs |
| 15:00 - 15:20 | break |
| 15:20 - 15:45 | Event Hubs |
| 15:45 - 17:00 | Event Grid |

## Time table (Two Day Version)

### First Day

| 09:00-10:15 | Storage Queues |
|---|---|
| 10:15 - 10:30 | break |
| 10:30 - 12:00 | Storage Queues |
| 12:00 - 13:00 | Lunch |
| 13:00 - 15:00 | Service Bus |
| 15:00 - 15:15 | break |
| 15:15 - 17:00 | Service Bus |

### Second Day

| 09:00-10:15 | Event Hubs |
|---|---|
| 10:15 - 10:30 | break |
| 10:30 - 12:00 | Event Hubs |
| 12:00 - 13:00 | Lunch |
| 13:00 - 15:00 | Event Grid |
| 15:00 - 15:15 | break |
| 15:15 - 17:00 | Event Grid |

## Connections

https://t.ly/mPMrU

## Reading material

### Event Hubs

- [Validate using an Avro schema when streaming events using Event Hubs .NET SDKs (AMQP)](https://learn.microsoft.com/en-us/azure/event-hubs/schema-registry-dotnet-send-receive-quickstart)
- [Schema Registry for Kafka Applications – Public Preview](https://techcommunity.microsoft.com/t5/messaging-on-azure-blog/json-schema-support-in-azure-event-hubs-schema-registry-for/ba-p/3825655)
- [Building A Custom Event Hubs Event Processor with .NET](https://devblogs.microsoft.com/azure-sdk/custom-event-processor/)
- [Samples](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/eventhub/Azure.Messaging.EventHubs/samples)
- [Event Processor Samples](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/eventhub/Azure.Messaging.EventHubs.Processor/samples)

### Event Grid

- [Debugging Azure Function Event Grid Triggers Locally with JetBrains Rider ](https://www.josephguadagno.net/2020/07/20/debugging-azure-function-event-grid-trigger-locally-with-jetbrains-rider)
- [Azure Service Bus vs Event Grid](https://yourazurecoach.com/2021/08/11/azure-service-bus-vs-event-grid/)
- [Azure Service Bus vs Event Grid Pull Delivery](https://yourazurecoach.com/2023/12/22/azure-service-bus-vs-event-grid-pull-delivery/)
- [Azure Service Bus vs Event Grid](https://yourazurecoach.com/2021/08/11/azure-service-bus-vs-event-grid/)
- [Azure Service Bus vs Event Grid Pull Delivery](https://yourazurecoach.com/2023/12/22/azure-service-bus-vs-event-grid-pull-delivery/)
