targetScope = 'resourceGroup'

@minLength(2)
@maxLength(50)
param functionAppName string
param location string
param tags object = {}

@secure()
param applicationSettings object

@secure()
param stagingApplicationSettings object = {}

var storageAccountName = 'st${uniqueString(resourceGroup().id, functionAppName)}'
var contentShareName = 'content${uniqueString(resourceGroup().id, functionAppName)}'
var stagingStorageAccountName = 'st${uniqueString(resourceGroup().id, functionAppName, 'Staging')}'
var stagingContentShareName = 'content${uniqueString(resourceGroup().id, functionAppName, 'Staging')}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {}
}

resource contentShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: contentShareName
  properties: {
    shareQuota: 5
  }
}

resource stagingStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: stagingStorageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
  }
}

resource stagingFileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: stagingStorage
  name: 'default'
  properties: {}
}

resource stagingContentShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: stagingFileService
  name: stagingContentShareName
  properties: {
    shareQuota: 5
  }
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${functionAppName}-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${functionAppName}-insights'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: '${functionAppName}-plan'
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false
  }
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var stagingStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${stagingStorage.name};AccountKey=${stagingStorage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var customSettings = [
  for setting in items(applicationSettings): {
    name: setting.key
    value: setting.value
  }
]
var stagingCustomSettings = [
  for setting in items(union(applicationSettings, stagingApplicationSettings)): {
    name: setting.key
    value: setting.value
  }
]
var stagingSlotSettingNames = [for setting in items(stagingApplicationSettings): setting.key]
var hostSettings = [
  {
    name: 'FUNCTIONS_EXTENSION_VERSION'
    value: '~4'
  }
  {
    name: 'FUNCTIONS_WORKER_RUNTIME'
    value: 'dotnet-isolated'
  }
  {
    name: 'WEBSITE_RUN_FROM_PACKAGE'
    value: '1'
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: insights.properties.ConnectionString
  }
]
var storageSettings = [
  {
    name: 'AzureWebJobsStorage'
    value: storageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: storageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: contentShare.name
  }
]
var stagingStorageSettings = [
  {
    name: 'AzureWebJobsStorage'
    value: stagingStorageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: stagingStorageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: stagingContentShare.name
  }
]

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      use32BitWorkerProcess: false
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      appSettings: concat(customSettings, hostSettings, storageSettings)
    }
  }
}

resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = {
  parent: functionApp
  name: 'Staging'
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      use32BitWorkerProcess: false
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      appSettings: concat(stagingCustomSettings, hostSettings, stagingStorageSettings)
    }
  }
}

resource slotSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: functionApp
  name: 'slotConfigNames'
  properties: {
    appSettingNames: concat([
      'AzureWebJobsStorage'
      'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    ], stagingSlotSettingNames)
  }
}

output functionAppName string = functionApp.name
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output principalId string = functionApp.identity.principalId
output storageAccountName string = storage.name
output stagingSlotName string = stagingSlot.name
output stagingSlotUrl string = 'https://${stagingSlot.properties.defaultHostName}'
output stagingPrincipalId string = stagingSlot.identity.principalId
