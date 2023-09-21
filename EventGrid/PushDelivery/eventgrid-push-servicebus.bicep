param location string = resourceGroup().location
param namespaceName string = 'ReliableMessagingServiceBus1'

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

#disable-next-line outputs-should-not-contain-secrets
output serviceBusConnectionString string = listKeys('${ServiceBus.id}/AuthorizationRules/RootManageSharedAccessKey', ServiceBus.apiVersion).primaryConnectionString
