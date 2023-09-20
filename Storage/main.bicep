param location string = resourceGroup().location
param storageNames array = [
  'reliablestorage3'
  'reliablestorage4'
]

resource StorageAccounts 'Microsoft.Storage/storageAccounts@2023-01-01' = [for name in storageNames: {
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  name: toLower(name)
  location: location
  tags: {}
  properties: {
    defaultToOAuthAuthentication: false
    allowCrossTenantReplication: false
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}]

output deployedAccounts array = [for (name, i) in storageNames: {
  name: name
  connectionString: 'DefaultEndpointsProtocol=https;AccountName=${name};AccountKey=${StorageAccounts[i].listKeys().keys[0].value};EndpointSuffix=core.windows.net'
}]
