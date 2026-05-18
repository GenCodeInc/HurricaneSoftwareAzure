param location string = 'centralus'
param functionAppName string = 'func-nhcparser-flex-${uniqueString(resourceGroup().id)}'
param appServicePlanName string = 'asp-nhcparser-flex-${uniqueString(resourceGroup().id)}'
param storageAccountName string = 'stnhcparser${uniqueString(resourceGroup().id)}'
@secure()
param sqlConnectionString string
param timerSchedule string = '0 */5 * * * *'
param currentYearOnly bool = true
param probeDatabaseOnStartup bool = false
param maximumInstanceCount int = 100
param instanceMemoryMB int = 512
param alwaysReadyFunctionName string = 'NHCParserTimer'
param alwaysReadyInstanceCount int = 1
param enableVnetIntegration bool = true
param vnetName string = 'vnet-nhcparser-${take(uniqueString(resourceGroup().id, functionAppName), 12)}'
param vnetAddressPrefix string = '10.20.0.0/24'
param integrationSubnetName string = 'snet-nhcparser-functions'
param integrationSubnetAddressPrefix string = '10.20.0.0/27'
param privateEndpointSubnetName string = 'snet-nhcparser-private-endpoints'
param privateEndpointSubnetAddressPrefix string = '10.20.0.32/27'
param sqlServerName string = ''
param sqlVirtualNetworkRuleName string = 'nhcparser-function-subnet'
param privateDnsZoneName string = 'privatelink${environment().suffixes.sqlServerHostname}'
param privateDnsZoneLinkName string = 'link-nhcparser-sql'
param sqlPrivateEndpointName string = 'pe-sql-${take(uniqueString(resourceGroup().id, sqlServerName), 8)}'
param sqlPrivateDnsZoneGroupName string = 'sql-dns-zone-group'
param vnetRouteAllEnabled bool = true

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var deploymentContainerName = 'app-package'
var logAnalyticsWorkspaceName = 'log-${take(uniqueString(resourceGroup().id, functionAppName), 20)}'
var applicationInsightsName = functionAppName

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }

  resource blobServices 'blobServices' = {
    name: 'default'

    resource deploymentContainer 'containers' = {
      name: deploymentContainerName
      properties: {
        publicAccess: 'None'
      }
    }
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-11-01' = if (enableVnetIntegration) {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressPrefix
      ]
    }
  }
}

resource integrationSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-11-01' = if (enableVnetIntegration) {
  parent: virtualNetwork
  name: integrationSubnetName
  properties: {
    addressPrefix: integrationSubnetAddressPrefix
    delegations: [
      {
        name: 'function-app-delegation'
        properties: {
          serviceName: 'Microsoft.Web/serverFarms'
        }
      }
    ]
    serviceEndpoints: [
      {
        service: 'Microsoft.Sql'
      }
    ]
  }
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-11-01' = if (enableVnetIntegration && !empty(sqlServerName)) {
  parent: virtualNetwork
  name: privateEndpointSubnetName
  properties: {
    addressPrefix: privateEndpointSubnetAddressPrefix
    privateEndpointNetworkPolicies: 'Disabled'
  }
}

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' existing = if (!empty(sqlServerName)) {
  name: sqlServerName
}

resource sqlVirtualNetworkRule 'Microsoft.Sql/servers/virtualNetworkRules@2025-02-01-preview' = if (enableVnetIntegration && !empty(sqlServerName)) {
  parent: sqlServer
  name: sqlVirtualNetworkRuleName
  properties: {
    ignoreMissingVnetServiceEndpoint: false
    virtualNetworkSubnetId: integrationSubnet.id
  }
}

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = if (enableVnetIntegration && !empty(sqlServerName)) {
  name: privateDnsZoneName
  location: 'global'
}

resource privateDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = if (enableVnetIntegration && !empty(sqlServerName)) {
  parent: privateDnsZone
  name: privateDnsZoneLinkName
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetwork.id
    }
  }
}

resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = if (enableVnetIntegration && !empty(sqlServerName)) {
  name: sqlPrivateEndpointName
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnet.id
    }
    privateLinkServiceConnections: [
      {
        name: 'sql-private-link'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: [
            'sqlServer'
          ]
        }
      }
    ]
  }
}

resource sqlPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = if (enableVnetIntegration && !empty(sqlServerName)) {
  parent: sqlPrivateEndpoint
  name: sqlPrivateDnsZoneGroupName
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'sql-zone-config'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    virtualNetworkSubnetId: enableVnetIntegration ? integrationSubnet.id : null
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
        alwaysReady: [
          {
            name: 'function:${alwaysReadyFunctionName}'
            instanceCount: alwaysReadyInstanceCount
          }
        ]
      }
    }
  }
}

resource functionWebConfig 'Microsoft.Web/sites/config@2024-04-01' = if (enableVnetIntegration) {
  name: 'web'
  parent: functionApp
  properties: {
    vnetRouteAllEnabled: vnetRouteAllEnabled
  }
}

resource functionAppSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  name: 'appsettings'
  parent: functionApp
  properties: {
    AzureWebJobsStorage: storageConnectionString
    DEPLOYMENT_STORAGE_CONNECTION_STRING: storageConnectionString
    APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString
    APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
    FUNCTIONS_EXTENSION_VERSION: '~4'
    NHC_TIMER_SCHEDULE: timerSchedule
    NHCParser__SqlConnectionString: sqlConnectionString
    NHCParser__CurrentYearOnly: string(currentYearOnly)
    NHCParser__ProbeDatabaseOnStartup: string(probeDatabaseOnStartup)
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
  }
}

output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
output storageAccountName string = storage.name
output appServicePlanName string = plan.name
output applicationInsightsName string = applicationInsights.name
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output virtualNetworkName string = enableVnetIntegration ? virtualNetwork.name : ''
output integrationSubnetResourceId string = enableVnetIntegration ? integrationSubnet.id : ''
output privateEndpointSubnetResourceId string = enableVnetIntegration && !empty(sqlServerName) ? privateEndpointSubnet.id : ''
output privateDnsZoneResourceId string = enableVnetIntegration && !empty(sqlServerName) ? privateDnsZone.id : ''
output sqlPrivateEndpointResourceId string = enableVnetIntegration && !empty(sqlServerName) ? sqlPrivateEndpoint.id : ''
