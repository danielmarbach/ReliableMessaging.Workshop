
param location string = resourceGroup().location
param eventHubNamespaceName string = 'reliablemessagingeventhubs2'
param eventHubNames array = [
  'topic1'
]
param blobStorageName string = 'reliablecheckpoints'

resource StorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  name: blobStorageName
  location: location
  tags: {}
  properties: {
    dnsEndpointType: 'Standard'
    defaultToOAuthAuthentication: false
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource BlobService 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  name: 'default'
  parent: StorageAccount
  properties: {
    automaticSnapshotPolicyEnabled: false
    containerDeleteRetentionPolicy: {
      allowPermanentDelete: true
    }
    deleteRetentionPolicy: {
      allowPermanentDelete: true
    }
    isVersioningEnabled: false
  }
}

resource Containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = [for eventHubName in eventHubNames: {
  name: eventHubName
  parent: BlobService
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    publicAccess: 'None'
  }
}]

resource EventHubsNamespace 'Microsoft.EventHub/namespaces@2023-01-01-preview' = {
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  name: eventHubNamespaceName
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

resource Topic 'Microsoft.EventHub/namespaces/eventhubs@2023-01-01-preview' = [for eventHubName in eventHubNames: {
  parent: EventHubsNamespace
  name: eventHubName
  properties: {
    retentionDescription: {
      cleanupPolicy: 'Delete'
      retentionTimeInHours: 2
    }
    messageRetentionInDays: 1
    partitionCount: 4
    status: 'Active'
  }
}]

#disable-next-line outputs-should-not-contain-secrets
output eventHubsConnectionString string = listKeys('${EventHubsNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', EventHubsNamespace.apiVersion).primaryConnectionString
#disable-next-line outputs-should-not-contain-secrets
output blobStorageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccount.name};AccountKey=${StorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

output deployedContainers array = [for (eventHubName, i) in eventHubNames: {
  name: eventHubName
  #disable-next-line outputs-should-not-contain-secrets
  connectionString: 'DefaultEndpointsProtocol=https;AccountName=${StorageAccount.name};AccountKey=${StorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
}]

output deployedEventHubs array = [for (eventHubName, i) in eventHubNames: {
  name: eventHubName
  #disable-next-line outputs-should-not-contain-secrets
  connectionString: listKeys('${EventHubsNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', EventHubsNamespace.apiVersion).primaryConnectionString
}]
