@description('Specifies the location for resources.')
param location string = resourceGroup().location
param namespaceName string = 'EventGridNamespace1'
param subscriptionName string = 'TemperatureChangedSubscription'
param topicName string = 'TemperatureChanged'

resource EventGridNamespace 'Microsoft.EventGrid/namespaces@2023-06-01-preview' = {
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
  name: namespaceName
}

resource EventGridTopic 'Microsoft.EventGrid/namespaces/topics@2023-06-01-preview' = {
  properties: {
    publisherType: 'Custom'
    inputSchema: 'CloudEventSchemaV1_0'
    eventRetentionInDays: 1
  }
  parent: EventGridNamespace
  name: topicName
}


resource EventGridSubscription 'Microsoft.EventGrid/namespaces/topics/eventSubscriptions@2023-06-01-preview' = {
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
  parent: EventGridTopic
  name: subscriptionName
}
