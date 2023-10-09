# Service Bus

## Exercises

### Processor (Simple)

Address all `// TODO` in the code.

Bonus exercise:
- Disable the forwarding on one of the subscriptions and try to introduce a new processor that receives from the subscription.
- Let's assume you'd have to throttle concurrency across all subscriptions on a processor that has multiple subscription receivers. How would you achieve that? Discuss with one of your peers the benefits and drawback of forwarding to the input queue from the subscription

### SessionProcessor (Simple)

Address all `// TODO` in the code.

Bonus exercise:

## Solutions

### Processor

#### Prerequisites

1. A fully deployed Service Bus namespace (use `servicebus.bicep` to deploy it) (adjust the necessary parameters or [create a parameter file](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/parameter-files))
1. Add the service bus connection string and service bus connection string without managed rights output to `appsettings.json` in the `ServiceBusOptions` section

#### Running it

`dotnet run -c Release`

### SessionProcessor

#### Prerequisites

1. A fully deployed Service Bus namespace (use `servicebus.bicep` to deploy it) (adjust the necessary parameters or [create a parameter file](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/parameter-files))
1. Add the service bus connection string and service bus connection string without managed rights output to `appsettings.json` in the `ServiceBusOptions` section

#### Running it

`dotnet run -c Release`