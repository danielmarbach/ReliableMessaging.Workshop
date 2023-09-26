param location string = resourceGroup().location
param serviceBusNames array = [
  'reliableservicebus1'
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

resource NoMangeRights 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' = [for (name, i) in serviceBusNames: {
  name: 'RootSharedAccessKey'
  parent: ServiceBuses[i]
  properties: {
    rights: [
      'Listen'
      'Send'
    ]
  }
}]

output deployedBuses array = [for (name, i) in serviceBusNames: {
  name: name
  #disable-next-line outputs-should-not-contain-secrets
  connectionString: listKeys('${ServiceBuses[i].id}/AuthorizationRules/RootManageSharedAccessKey', ServiceBuses[i].apiVersion).primaryConnectionString
  #disable-next-line outputs-should-not-contain-secrets
  connectionStringNoManageRights: listKeys('${ServiceBuses[i].id}/AuthorizationRules/RootSharedAccessKey', ServiceBuses[i].apiVersion).primaryConnectionString
}]
