// Azure SQL Database Module
param location string
param environment string
param resourcePrefix string
param tags object
param subnetId string

var sqlServerName = '${resourcePrefix}-sql-${environment}'
var databaseName = 'BankingDB'

resource sqlServer 'Microsoft.Sql/servers@2023-02-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: 'CHANGE_ME_IN_KEYVAULT'  // Should be stored in Key Vault
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    restrictOutboundNetworkAccess: 'Enabled'
  }
  
  resource database 'databases@2023-02-01-preview' = {
    name: databaseName
    location: location
    tags: tags
    sku: {
      name: 'S3'
      tier: 'Standard'
    }
    properties: {
      collation: 'SQL_Latin1_General_CP1_CI_AS'
      maxSizeBytes: 268435456000  // 250 GB
      zoneRedundant: true
      readScale: 'Disabled'
      requestedBackupStorageRedundancy: 'Geo'
    }
  }
  
  resource firewallRule 'firewallRules@2023-02-01-preview' = {
    name: 'AllowAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

// Private endpoint for SQL
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: '${sqlServerName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${sqlServerName}-pe-connection'
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

// Diagnostic settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'sql-diagnostics'
  scope: sqlServer::database
  properties: {
    workspaceId: resourceId('Microsoft.OperationalInsights/workspaces', '${resourcePrefix}-log-${environment}')
    logs: [
      {
        category: 'SQLInsights'
        enabled: true
      }
      {
        category: 'QueryStoreRuntimeStatistics'
        enabled: true
      }
      {
        category: 'QueryStoreWaitStatistics'
        enabled: true
      }
      {
        category: 'Errors'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output sqlServerId string = sqlServer.id
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = databaseName
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;'
