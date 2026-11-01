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

@description('Use the same password for ExecutionForecast and GridCarbonIntensity so API keys work on both APIs.')
@secure()
@minLength(1)
param apiKeyPassword string

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
      ApplicationSettings__ApiKeyPassword: apiKeyPassword
    }
  }
}

output functionAppName string = app.outputs.functionAppName
output functionAppUrl string = app.outputs.functionAppUrl
