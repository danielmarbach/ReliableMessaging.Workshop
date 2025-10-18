param location string = resourceGroup().location
param storageAccountName string = 'testreliable'
param storageAccountSystemTopic string = 'testreliablesystemtopic'
param eventGridNamespaceName string = 'testreliableeventgridnamespace'

resource EventGridNamespace 'Microsoft.EventGrid/namespaces@2023-12-15-preview' = {
  name: eventGridNamespaceName
  location: location
  sku: {
    name: 'Standard'
    capacity: 1
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    topicsConfiguration: {}
    isZoneRedundant: true
    publicNetworkAccess: 'Enabled'
    inboundIpRules: []
  }
}

resource StorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  properties: {
    dnsEndpointType: 'Standard'
    defaultToOAuthAuthentication: false
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      requireInfrastructureEncryption: false
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource EventGridNamespaceTopic 'Microsoft.EventGrid/namespaces/topics@2023-12-15-preview' = {
  parent: EventGridNamespace
  name: 'testtopic'
  properties: {
    publisherType: 'Custom'
    inputSchema: 'CloudEventSchemaV1_0'
    eventRetentionInDays: 7
  }
}

resource SystemTopic 'Microsoft.EventGrid/systemTopics@2023-12-15-preview' = {
  name: storageAccountSystemTopic
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    source: StorageAccount.id
    topicType: 'Microsoft.Storage.StorageAccounts'
  }
}

resource EventGridNamespaceTopicSubscription 'Microsoft.EventGrid/namespaces/topics/eventSubscriptions@2023-12-15-preview' = {
  parent: EventGridNamespaceTopic
  name: 'testsubscription'
  properties: {
    deliveryConfiguration: {
      deliveryMode: 'Queue'
      queue: {
        receiveLockDurationInSeconds: 60
        maxDeliveryCount: 10
        eventTimeToLive: 'P7D'
      }
    }
    eventDeliverySchema: 'CloudEventSchemaV1_0'
    filtersConfiguration: {
      includedEventTypes: []
    }
  }
}

resource eventGridDataSenderRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: 'd5a91429-5739-47e2-a06b-3470a27159e7'
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: EventGridNamespaceTopic
  name: guid(EventGridNamespaceTopic.id, SystemTopic.id, eventGridDataSenderRoleDefinition.name)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', eventGridDataSenderRoleDefinition.name)
    principalId: SystemTopic.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource SystemTopicSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2023-12-15-preview' = {
  parent: SystemTopic
  name: '${SystemTopic.name}Subscription'
  properties: {
    deliveryWithResourceIdentity: {
      identity: {
        type: 'SystemAssigned'
      }
      destination: {
        properties: {
          resourceId: EventGridNamespaceTopic.id
        }
        endpointType: 'NamespaceTopic'
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.Storage.BlobCreated'
        'Microsoft.Storage.BlobDeleted'
      ]
      enableAdvancedFilteringOnArrays: true
    }
    labels: []
    eventDeliverySchema: 'CloudEventSchemaV1_0'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
  dependsOn: [
    roleAssignment  // Wait for the role assignment to complete
  ]
}

var eventGridAccessKey = EventGridNamespace.listKeys().key1

output eventGridAccessKey string = eventGridAccessKey
output eventGridName string = EventGridNamespace.name
output eventGridHostName string = 'https://${EventGridNamespace.properties.topicsConfiguration.hostname}'
#disable-next-line outputs-should-not-contain-secrets
output blobStorageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccount.name};AccountKey=${StorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
