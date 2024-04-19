
param location string = resourceGroup().location
param eventHubNamespaceName string = 'reliablemessagingeventhubsdemo'
param eventHubName string = 'topicdemo'
param blobStorageName string = 'reliablecheckpointsdemo'

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

resource Containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  name: eventHubName
  parent: BlobService
  properties: {
    immutableStorageWithVersioning: {
      enabled: false
    }
    publicAccess: 'None'
  }
}

resource EventHubsNamespace 'Microsoft.EventHub/namespaces@2023-01-01-preview' = {
  sku: {
    name: 'Premium'
    tier: 'Premium'
    capacity: 1
  }
  identity: {
    type: 'SystemAssigned'
  }
  name: eventHubNamespaceName
  location: location
  tags: {}
  properties: {
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: true
    kafkaEnabled: true
  }
}

resource Topic 'Microsoft.EventHub/namespaces/eventhubs@2023-01-01-preview' = {
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
}

resource storageBlobDataContributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

resource storageBlobDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: Topic
  name: guid(EventHubsNamespace.id, Topic.name, storageBlobDataContributor.name)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributor.name)
    principalId: EventHubsNamespace.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource schemaRegistryReader 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '2c56ea50-c6b3-40a6-83c0-9d98858bc7d2'
}

resource schemaRegistryReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: Topic
  name: guid(EventHubsNamespace.id, Topic.name, schemaRegistryReader.name)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', schemaRegistryReader.name)
    principalId: EventHubsNamespace.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource SchemaGroup 'Microsoft.EventHub/namespaces/schemagroups@2023-01-01-preview' = {
  parent: EventHubsNamespace
  name: 'Schemas'
  properties: {
    groupProperties: {}
    schemaCompatibility: 'None'
    schemaType: 'Avro'
  }
}

#disable-next-line outputs-should-not-contain-secrets
output eventHubsConnectionString string = listKeys('${EventHubsNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', EventHubsNamespace.apiVersion).primaryConnectionString
#disable-next-line outputs-should-not-contain-secrets
output blobStorageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${StorageAccount.name};AccountKey=${StorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
