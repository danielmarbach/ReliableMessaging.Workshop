param location string = resourceGroup().location
param serviceBusNames array = [
  'reliableservicebus1'
  'reliableservicebus2'
]

resource ServiceBuses 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = [for name in serviceBusNames: {
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  name: toLower(name)
  location: location
  tags: {}
  properties: {
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}]

output deployedBuses array = [for (name, i) in serviceBusNames: {
  name: name
  #disable-next-line outputs-should-not-contain-secrets
  connectionString: listKeys('${ServiceBuses[i].id}/AuthorizationRules/RootManageSharedAccessKey', ServiceBuses[i].apiVersion).primaryConnectionString
}]
