// Role Assignments Module
param aksIdentityPrincipalId string
param acrId string
param keyVaultId string

// AKS to ACR - AcrPull role
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrId, aksIdentityPrincipalId, 'AcrPull')
  scope: resourceId('Microsoft.ContainerRegistry/registries', split(acrId, '/')[8])
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull
    principalId: aksIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// AKS to Key Vault - Key Vault Secrets User role
resource kvSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultId, aksIdentityPrincipalId, 'KeyVaultSecretsUser')
  scope: resourceId('Microsoft.KeyVault/vaults', split(keyVaultId, '/')[8])
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: aksIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output acrRoleAssignmentId string = acrPullRole.id
output kvRoleAssignmentId string = kvSecretsUserRole.id
