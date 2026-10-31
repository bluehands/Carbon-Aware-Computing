using './forecast-updater.bicep'

param functionAppName = 'cac-forecast-updater-dev'
param location = 'westeurope'
param tags = {
  application: 'Carbon-Aware-Computing'
  environment: 'dev'
}
param mailFrom = readEnvironmentVariable('CAC_MAIL_FROM')
param tenantId = readEnvironmentVariable('CAC_TENANT_ID')
param clientId = readEnvironmentVariable('CAC_CLIENT_ID')
param clientSecret = readEnvironmentVariable('CAC_CLIENT_SECRET')
param forecastStorageResourceGroup = 'Carbon-Aware-Computing'
param writeHistoryFor = ''
