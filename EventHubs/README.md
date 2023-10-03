# Event Hubs

## Exercises

### Processor (Simple)

Address all `// TODO` in the code.

Bonus exercise:

## Solutions

### Processor

#### Prerequisites

1. A fully deployed Event Hub with an Azure Storage Account (use `eventhubs.bicep` to deploy it) (adjust the necessary parameters or [create a parameter file](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/parameter-files))
1. Add the event hub connection string and event hub name output to `appsettings.json` in the `EventHubs` section
1. Add the blob storage connection string output to `appsettings.json` in the `StorageOptions` section. Make sure to set a unique container name

#### Running it

`dotnet run -c Release`