
param location string = resourceGroup().location
param namespaceName string = 'ReliableMessagingServiceBus1'
param queueName string = 'ReliableMessagingServiceBus1Queue1'
param topicName string = 'ReliableMessagingServiceBus1Topic1'
param subscriptionName string = 'ReliableMessagingServiceBus1TopicSubscription'
param endpointUrl string = 'https://xyz.ngrok-free.app/api/EventGridEventHandler'

resource ServiceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: namespaceName
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: ServiceBus
  name: queueName
  properties: {
    maxSizeInMegabytes: 1024
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
      advancedFilters: [
        {
          values: [
            queueName
          ]
          operatorType: 'StringIn'
          key: 'data.QueueName'
        }
      ]
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
