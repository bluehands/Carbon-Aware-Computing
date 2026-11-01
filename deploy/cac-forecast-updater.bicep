targetScope = 'resourceGroup'

@minLength(2)
@maxLength(50)
param functionAppName string
param location string = resourceGroup().location
param tags object = {}
param mailFrom string
param tenantId string
param clientId string

@secure()
@minLength(1)
param clientSecret string

@description('Resource group of the existing carbonawarecomputing Storage Account, in the deployment subscription.')
param forecastStorageResourceGroup string

@description('Comma-separated computing location names for which forecast history is written.')
param writeHistoryFor string = ''

module app './modules/cac-windows-consumption-function.bicep' = {
  name: '${functionAppName}-infra'
  params: {
    functionAppName: functionAppName
    location: location
    tags: tags
    applicationSettings: {
      ApplicationSettings__MailFrom: mailFrom
      ApplicationSettings__TenantId: tenantId
      ApplicationSettings__ClientId: clientId
      ApplicationSettings__ClientSecret: clientSecret
      ApplicationSettings__WriteHistoryFor: writeHistoryFor
      'AzureWebJobs.ScheduledUpdateForecast.Disabled': 'false'
      'AzureWebJobs.ScheduledReportForecast.Disabled': 'false'
    }
    stagingApplicationSettings: {
      'AzureWebJobs.ScheduledUpdateForecast.Disabled': 'true'
      'AzureWebJobs.ScheduledReportForecast.Disabled': 'true'
    }
  }
}

module forecastStorageAccess './modules/cac-forecast-storage-access.bicep' = {
  name: '${functionAppName}-storage-rbac'
  scope: resourceGroup(forecastStorageResourceGroup)
  params: {
    principalId: app.outputs.principalId
  }
}

module stagingForecastStorageAccess './modules/cac-forecast-storage-access.bicep' = {
  name: '${functionAppName}-staging-rbac'
  scope: resourceGroup(forecastStorageResourceGroup)
  params: {
    principalId: app.outputs.stagingPrincipalId
  }
}

output functionAppName string = app.outputs.functionAppName
output functionAppUrl string = app.outputs.functionAppUrl
output stagingSlotName string = app.outputs.stagingSlotName
output stagingSlotUrl string = app.outputs.stagingSlotUrl
