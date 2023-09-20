
param location string = resourceGroup().location
param eventHubName string = 'reliablemessagingeventhubs2'
param topicName string = 'topic1'

resource EventHubsNamespace 'Microsoft.EventHub/namespaces@2023-01-01-preview' = {
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  name: eventHubName
  location: location
  tags: {}
  properties: {
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: true
    isAutoInflateEnabled: true
    maximumThroughputUnits: 10
    kafkaEnabled: true
  }
}

resource topic 'Microsoft.EventHub/namespaces/eventhubs@2023-01-01-preview' = {
  parent: EventHubsNamespace
  name: topicName
  properties: {
    retentionDescription: {
      cleanupPolicy: 'Delete'
      retentionTimeInHours: 2
    }
    messageRetentionInDays: 1
    partitionCount: 4
    status: 'Active'
  }
}


resource Topic 'Microsoft.EventHub/namespaces/schemagroups@2023-01-01-preview' = {
  parent: EventHubsNamespace
  name: 'Schemas'
  properties: {
    groupProperties: {}
    schemaCompatibility: 'None'
    schemaType: 'Json'
  }
}
