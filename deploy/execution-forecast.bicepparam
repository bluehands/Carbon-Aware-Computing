using './execution-forecast.bicep'

param functionAppName = 'cac-execution-forecast-dev'
param location = 'westeurope'
param tags = {
  application: 'Carbon-Aware-Computing'
  environment: 'dev'
}
param mailFrom = readEnvironmentVariable('CAC_MAIL_FROM')
param tenantId = readEnvironmentVariable('CAC_TENANT_ID')
param clientId = readEnvironmentVariable('CAC_CLIENT_ID')
param clientSecret = readEnvironmentVariable('CAC_CLIENT_SECRET')
param apiKeyPassword = readEnvironmentVariable('CAC_API_KEY_PASSWORD')
