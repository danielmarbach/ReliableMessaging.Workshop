
param location string = resourceGroup().location
param namespaceName string = 'ReliableMessagingServiceBus1'
param topicName string = 'ReliableMessagingServiceBus1Topic'
param subscriptionName string = 'ReliableMessagingServiceBus1TopicSubscription'
param endpointUrl string = 'https://XYZ.ngrok-free.app/api/EventGridEventHandler'

resource ServiceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  sku: {
    name: 'Premium'
    tier: 'Premium'
    capacity: 1
  }
  name: namespaceName
  location: location
  tags: {}
  properties: {
    premiumMessagingPartitions: 1
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: true
  }
}

resource EventGridTopic 'Microsoft.EventGrid/systemTopics@2023-06-01-preview' = {
  properties: {
    source: ServiceBus.id
    topicType: 'Microsoft.ServiceBus.Namespaces'
  }
  identity: {
    type: 'None'
  }
  location: location
  tags: {}
  name: topicName
}

resource EventGridSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2023-06-01-preview' = {
  properties: {
    destination: {
      endpointType: 'WebHook'
      properties: {
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
        endpointUrl: endpointUrl
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.ServiceBus.ActiveMessagesAvailableWithNoListeners'
      ]
      enableAdvancedFilteringOnArrays: true
    }
    eventDeliverySchema: 'CloudEventSchemaV1_0'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
  name: subscriptionName
  parent: EventGridTopic
}
