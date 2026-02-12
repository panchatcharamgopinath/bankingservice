// Main Bicep template for Banking Service Infrastructure
targetScope = 'resourceGroup'

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Environment name')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'prod'

@description('Resource name prefix')
param resourcePrefix string = 'banking'

@description('Tags to apply to all resources')
param tags object = {
  Application: 'Banking Service'
  Environment: environment
  ManagedBy: 'Bicep'
  CostCenter: 'Engineering'
  Compliance: 'PCI-DSS'
}

// Network Module
module network './modules/network.bicep' = {
  name: 'network-deployment'
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    tags: tags
  }
}

// Log Analytics Module
module logAnalytics './modules/loganalytics.bicep' = {
  name: 'loganalytics-deployment'
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    tags: tags
  }
}

// Container Registry Module
module acr './modules/acr.bicep' = {
  name: 'acr-deployment'
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    tags: tags
  }
}

// Key Vault Module
module keyVault './modules/keyvault.bicep' = {
  name: 'keyvault-deployment'
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    tags: tags
  }
}

// SQL Database Module
module sql './modules/sql.bicep' = {
  name: 'sql-deployment'
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    tags: tags
    subnetId: network.outputs.sqlSubnetId
  }
}

// AKS Module
module aks './modules/aks.bicep' = {
  name: 'aks-deployment'
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    tags: tags
    subnetId: network.outputs.aksSubnetId
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    acrId: acr.outputs.acrId
    keyVaultId: keyVault.outputs.keyVaultId
  }
}

// Role Assignments Module
module roleAssignments './modules/roleassignments.bicep' = {
  name: 'roleassignments-deployment'
  params: {
    aksIdentityPrincipalId: aks.outputs.kubeletIdentityPrincipalId
    acrId: acr.outputs.acrId
    keyVaultId: keyVault.outputs.keyVaultId
  }
}

// Outputs
output aksClusterName string = aks.outputs.clusterName
output acrName string = acr.outputs.acrName
output keyVaultName string = keyVault.outputs.keyVaultName
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output vnetId string = network.outputs.vnetId
