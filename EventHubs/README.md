# Event Hubs

## Exercises

### Processor (Simple)

Address all `// TODO` in the code.

Bonus exercise:

- Play around with the channels
- What are the implications of using the channel as a partition key? What would happen if you don't use the channel as a partition key? Are there better ways to partition the data? Discuss your findings it with one of your peers.
- Set `ProduceData` to `false` in the `SenderOptions`, process all data. Restart and see what happens. The set the `RestartFromBeginning` to `true`. Do the same spiel again but this time change the threshold. Discuss your findings it with one of your peers.

## Solutions

### Processor

#### Prerequisites

1. A fully deployed Event Hub with an Azure Storage Account (use `eventhubs.bicep` to deploy it) (adjust the necessary parameters or [create a parameter file](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/parameter-files))
1. Add the event hub connection string and event hub name output to `appsettings.json` in the `EventHubs` section
1. Add the blob storage connection string output to `appsettings.json` in the `StorageOptions` section. Make sure to set a unique container name

#### Running it

`dotnet run -c Release`