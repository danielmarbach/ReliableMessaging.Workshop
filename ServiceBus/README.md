# Service Bus

## Exercises

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