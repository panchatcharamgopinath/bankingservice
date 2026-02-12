// Azure Kubernetes Service Module
param location string
param environment string
param resourcePrefix string
param tags object
param subnetId string
param logAnalyticsWorkspaceId string
param acrId string
param keyVaultId string

var aksName = '${resourcePrefix}-aks-${environment}'
var nodeResourceGroupName = 'MC_${resourcePrefix}-${environment}'

resource aks 'Microsoft.ContainerService/managedClusters@2023-10-01' = {
  name: aksName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: '1.28'
    dnsPrefix: '${resourcePrefix}-${environment}'
    
    // Node pools
    agentPoolProfiles: [
      {
        name: 'system'
        count: 3
        vmSize: 'Standard_D4s_v5'
        osType: 'Linux'
        mode: 'System'
        type: 'VirtualMachineScaleSets'
        vnetSubnetID: subnetId
        availabilityZones: [
          '1'
          '2'
          '3'
        ]
        enableAutoScaling: true
        minCount: 3
        maxCount: 10
        maxPods: 50
        osDiskSizeGB: 128
        osDiskType: 'Ephemeral'
      }
      {
        name: 'user'
        count: 3
        vmSize: 'Standard_D8s_v5'
        osType: 'Linux'
        mode: 'User'
        type: 'VirtualMachineScaleSets'
        vnetSubnetID: subnetId
        availabilityZones: [
          '1'
          '2'
          '3'
        ]
        enableAutoScaling: true
        minCount: 3
        maxCount: 20
        maxPods: 50
        osDiskSizeGB: 128
        osDiskType: 'Ephemeral'
        nodeTaints: [
          'workload=application:NoSchedule'
        ]
      }
    ]
    
    // Network profile
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'calico'
      serviceCidr: '10.1.0.0/16'
      dnsServiceIP: '10.1.0.10'
      loadBalancerSku: 'standard'
      outboundType: 'loadBalancer'
    }
    
    // Add-ons
    addonProfiles: {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId
        }
      }
      azureKeyvaultSecretsProvider: {
        enabled: true
        config: {
          enableSecretRotation: 'true'
          rotationPollInterval: '2m'
        }
      }
      azurepolicy: {
        enabled: true
      }
    }
    
    // Security
    aadProfile: {
      managed: true
      enableAzureRBAC: true
    }
    
    apiServerAccessProfile: {
      enablePrivateCluster: true
    }
    
    // Auto-upgrade
    autoUpgradeProfile: {
      upgradeChannel: 'stable'
    }
    
    // Security profile
    securityProfile: {
      defender: {
        logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceId
        securityMonitoring: {
          enabled: true
        }
      }
      workloadIdentity: {
        enabled: true
      }
    }
    
    // Other settings
    disableLocalAccounts: true
    enableRBAC: true
    nodeResourceGroup: nodeResourceGroupName
  }
}

output clusterName string = aks.name
output clusterId string = aks.id
output kubeletIdentityObjectId string = aks.properties.identityProfile.kubeletidentity.objectId
output kubeletIdentityPrincipalId string = aks.identity.principalId
output oidcIssuerUrl string = aks.properties.oidcIssuerProfile.issuerURL
