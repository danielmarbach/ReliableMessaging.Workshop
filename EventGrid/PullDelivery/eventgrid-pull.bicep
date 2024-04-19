param location string = resourceGroup().location
param eventGridNamespaceNames array = [
  'EventGridNamespace1'
]
param subscriptionName string = 'TemperatureChangedSubscription'
param topicName string = 'TemperatureChanged'

resource EventGridNamespace 'Microsoft.EventGrid/namespaces@2023-06-01-preview' = [for name in eventGridNamespaceNames: {
  properties: {
    topicsConfiguration: {}
    isZoneRedundant: true
    publicNetworkAccess: 'Enabled'
    inboundIpRules: []
  }
  sku: {
    name: 'Standard'
    capacity: 1
  }
  identity: {
    type: 'None'
  }
  location: location
  tags: {}
  name: toLower(name)
}]

resource EventGridTopic 'Microsoft.EventGrid/namespaces/topics@2023-06-01-preview' = [for (name, i) in eventGridNamespaceNames: {
  properties: {
    publisherType: 'Custom'
    inputSchema: 'CloudEventSchemaV1_0'
    eventRetentionInDays: 1
  }
  parent: EventGridNamespace[i]
  name: topicName
}]

resource EventGridSubscription 'Microsoft.EventGrid/namespaces/topics/eventSubscriptions@2023-06-01-preview' = [for (name, i) in eventGridNamespaceNames: {
  properties: {
    deliveryConfiguration: {
      deliveryMode: 'Queue'
      queue: {
        receiveLockDurationInSeconds: 60
        maxDeliveryCount: 10
        eventTimeToLive: 'P1D'
      }
    }
    eventDeliverySchema: 'CloudEventSchemaV1_0'
    filtersConfiguration: {
      includedEventTypes: []
    }
  }
  parent: EventGridTopic[i]
  name: subscriptionName
}]

output deployedEventGridNamespaces array = [for (name, i) in eventGridNamespaceNames: {
  name: name
  #disable-next-line outputs-should-not-contain-secrets
  eventGridAccessKey: EventGridNamespace[i].listKeys().key1
  eventGridHostName: 'https://${EventGridNamespace[i].properties.topicsConfiguration.hostname}'
}]
