# Banking Service - Production Deployment Guide

**Version:** 1.0  
**Date:** January 16, 2026  
**Estimated Deployment Time:** 2-3 hours

---

## Prerequisites

### Required Tools
```bash
# Azure CLI
az --version  # >= 2.50.0

# kubectl
kubectl version --client  # >= 1.28.0

# Docker
docker --version  # >= 24.0.0

# Helm (optional)
helm version  # >= 3.12.0
```

### Required Permissions
- **Azure:** Contributor + User Access Administrator on subscription
- **AKS:** Cluster Admin role
- **Key Vault:** Key Vault Administrator role

### Environment Variables
```bash
export AZURE_SUBSCRIPTION_ID="YOUR_SUBSCRIPTION_ID"
export RESOURCE_GROUP="banking-rg-prod"
export LOCATION="eastus"
export ENVIRONMENT="prod"
```

---

## Phase 1: Infrastructure Deployment (45 minutes)

### Step 1.1: Create Resource Group
```bash
az account set --subscription $AZURE_SUBSCRIPTION_ID

az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --tags \
    Environment=$ENVIRONMENT \
    Application="Banking Service" \
    ManagedBy="Bicep"
```

### Step 1.2: Deploy Infrastructure
```bash
cd infra

# Validate template
az deployment group validate \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters \
    location=$LOCATION \
    environment=$ENVIRONMENT \
    resourcePrefix="banking"

# Deploy (takes ~30-40 minutes)
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters \
    location=$LOCATION \
    environment=$ENVIRONMENT \
    resourcePrefix="banking" \
  --name "banking-infra-$(date +%Y%m%d-%H%M%S)"

# Capture outputs
export ACR_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name banking-infra-* \
  --query properties.outputs.acrName.value -o tsv)

export AKS_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name banking-infra-* \
  --query properties.outputs.aksClusterName.value -o tsv)

export KV_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name banking-infra-* \
  --query properties.outputs.keyVaultName.value -o tsv)

export SQL_SERVER=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name banking-infra-* \
  --query properties.outputs.sqlServerFqdn.value -o tsv)
```

### Step 1.3: Verify Infrastructure
```bash
# Check all resources are created
az resource list \
  --resource-group $RESOURCE_GROUP \
  --output table

# Expected resources:
# - Virtual Network
# - AKS Cluster
# - Container Registry
# - Key Vault
# - SQL Server + Database
# - Log Analytics Workspace
# - Application Insights
```

---

## Phase 2: Database Setup (15 minutes)

### Step 2.1: Generate Strong Passwords
```bash
# Generate SQL admin password
SQL_ADMIN_PASSWORD=$(openssl rand -base64 32)
echo "SQL Admin Password (save securely): $SQL_ADMIN_PASSWORD"

# Generate JWT signing key
JWT_KEY=$(openssl rand -base64 64)
echo "JWT Key (save securely): $JWT_KEY"
```

### Step 2.2: Configure SQL Database
```bash
# Update SQL admin password (if not done during deployment)
az sql server update \
  --resource-group $RESOURCE_GROUP \
  --name ${SQL_SERVER%%.*} \
  --admin-password "$SQL_ADMIN_PASSWORD"

# Get connection string
SQL_CONNECTION_STRING="Server=tcp:$SQL_SERVER,1433;Database=BankingDB;User ID=sqladmin;Password=$SQL_ADMIN_PASSWORD;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"

echo "Connection String: $SQL_CONNECTION_STRING"
```

### Step 2.3: Run Database Migrations
```bash
# Option 1: Run locally (requires VPN/firewall rule)
cd ../src
dotnet ef database update --connection "$SQL_CONNECTION_STRING"

# Option 2: Use Azure SQL Query Editor or SSMS
# - Connect to SQL Server
# - Run migration scripts from src/Migrations/
```

---

## Phase 3: Secrets Configuration (10 minutes)

### Step 3.1: Get Key Vault Name
```bash
echo "Key Vault: $KV_NAME"
```

### Step 3.2: Store Secrets in Key Vault
```bash
# SQL Connection String
az keyvault secret set \
  --vault-name $KV_NAME \
  --name banking-sql-connection-string \
  --value "$SQL_CONNECTION_STRING"

# JWT Signing Key
az keyvault secret set \
  --vault-name $KV_NAME \
  --name banking-jwt-key \
  --value "$JWT_KEY"

# Get Application Insights Connection String
APP_INSIGHTS_CS=$(az monitor app-insights component show \
  --resource-group $RESOURCE_GROUP \
  --app banking-ai-$ENVIRONMENT \
  --query connectionString -o tsv)

az keyvault secret set \
  --vault-name $KV_NAME \
  --name banking-appinsights-connection-string \
  --value "$APP_INSIGHTS_CS"
```

### Step 3.3: Verify Secrets
```bash
az keyvault secret list \
  --vault-name $KV_NAME \
  --output table
```

---

## Phase 4: AKS Configuration (20 minutes)

### Step 4.1: Get AKS Credentials
```bash
az aks get-credentials \
  --resource-group $RESOURCE_GROUP \
  --name $AKS_NAME \
  --overwrite-existing

# Verify connection
kubectl get nodes
```

### Step 4.2: Install Required Add-ons

#### Install NGINX Ingress Controller
```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz \
  --set controller.metrics.enabled=true \
  --set controller.podAnnotations."prometheus\.io/scrape"=true \
  --set controller.podAnnotations."prometheus\.io/port"=10254
```

#### Install Cert-Manager
```bash
helm repo add jetstack https://charts.jetstack.io
helm repo update

helm install cert-manager jetstack/cert-manager \
  --namespace cert-manager \
  --create-namespace \
  --set installCRDs=true
```

#### Configure Let's Encrypt Issuer
```bash
cat <<EOF | kubectl apply -f -
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: platform-team@yourcompany.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

### Step 4.3: Configure Azure Workload Identity
```bash
# Get AKS OIDC Issuer URL
export OIDC_ISSUER=$(az aks show \
  --resource-group $RESOURCE_GROUP \
  --name $AKS_NAME \
  --query oidcIssuerProfile.issuerUrl -o tsv)

# Create Managed Identity
az identity create \
  --resource-group $RESOURCE_GROUP \
  --name banking-workload-identity

export MI_CLIENT_ID=$(az identity show \
  --resource-group $RESOURCE_GROUP \
  --name banking-workload-identity \
  --query clientId -o tsv)

export MI_PRINCIPAL_ID=$(az identity show \
  --resource-group $RESOURCE_GROUP \
  --name banking-workload-identity \
  --query principalId -o tsv)

# Grant Key Vault access to Managed Identity
az keyvault set-policy \
  --name $KV_NAME \
  --object-id $MI_PRINCIPAL_ID \
  --secret-permissions get list

# Create federated credential
az identity federated-credential create \
  --name banking-federated-credential \
  --identity-name banking-workload-identity \
  --resource-group $RESOURCE_GROUP \
  --issuer $OIDC_ISSUER \
  --subject system:serviceaccount:banking:banking-api-sa \
  --audience api://AzureADTokenExchange
```

---

## Phase 5: Application Deployment (30 minutes)

### Step 5.1: Build and Push Docker Image
```bash
cd ..

# Login to ACR
az acr login --name $ACR_NAME

# Build image
docker build \
  -f docker/Dockerfile \
  -t $ACR_NAME.azurecr.io/banking-api:v1.0.0 \
  -t $ACR_NAME.azurecr.io/banking-api:latest \
  .

# Push image
docker push $ACR_NAME.azurecr.io/banking-api:v1.0.0
docker push $ACR_NAME.azurecr.io/banking-api:latest

# Scan image (optional but recommended)
az acr task run \
  --registry $ACR_NAME \
  --name quick-scan \
  --image banking-api:v1.0.0
```

### Step 5.2: Update Kubernetes Manifests
```bash
cd k8s

# Update image references
sed -i "s/REPLACE_WITH_ACR_NAME/$ACR_NAME/g" *.yaml
sed -i "s/REPLACE_WITH_IMAGE_TAG/v1.0.0/g" *.yaml

# Update managed identity
sed -i "s/REPLACE_WITH_MANAGED_IDENTITY_CLIENT_ID/$MI_CLIENT_ID/g" *.yaml

# Update Key Vault name
sed -i "s/REPLACE_WITH_KEYVAULT_NAME/$KV_NAME/g" *.yaml

# Update tenant ID
TENANT_ID=$(az account show --query tenantId -o tsv)
sed -i "s/REPLACE_WITH_TENANT_ID/$TENANT_ID/g" *.yaml
```

### Step 5.3: Deploy to Kubernetes
```bash
# Apply manifests in order
kubectl apply -f 01-namespace.yaml
kubectl apply -f 02-serviceaccount.yaml
kubectl apply -f 03-configmap.yaml
kubectl apply -f 04-secretproviderclass.yaml
kubectl apply -f 05-deployment.yaml
kubectl apply -f 06-service.yaml
kubectl apply -f 07-hpa.yaml
kubectl apply -f 08-pdb.yaml
kubectl apply -f 09-networkpolicy.yaml
kubectl apply -f 10-ingress.yaml
```

### Step 5.4: Wait for Deployment
```bash
# Watch pods come up
kubectl get pods -n banking -w

# Expected: 3 pods in Running state after 2-3 minutes
```

---

## Phase 6: Verification (15 minutes)

### Step 6.1: Check Pod Status
```bash
kubectl get all -n banking

# Expected output:
# - 3/3 pods Running
# - Service created
# - Deployment ready
```

### Step 6.2: Check Logs
```bash
# View application logs
kubectl logs -n banking -l app=banking-api --tail=50

# Should show:
# - "Banking Service starting up..."
# - "Database initialized successfully"
# - No errors
```

### Step 6.3: Verify Secrets Mounted
```bash
# Exec into a pod
POD_NAME=$(kubectl get pods -n banking -l app=banking-api -o jsonpath='{.items[0].metadata.name}')

kubectl exec -n banking $POD_NAME -- ls -la /mnt/secrets-store

# Expected:
# - banking-sql-connection-string
# - banking-jwt-key
# - banking-appinsights-connection-string
```

### Step 6.4: Test Health Endpoints
```bash
# Port forward to test locally
kubectl port-forward -n banking svc/banking-api 8080:80 &

# Test health
curl http://localhost:8080/health
# Expected: {"status": "Healthy", ...}

curl http://localhost:8080/ready
# Expected: {"status": "Healthy", ...}

# Kill port forward
pkill -f "port-forward"
```

### Step 6.5: Test External Access
```bash
# Get ingress IP
kubectl get ingress -n banking

# Wait for EXTERNAL-IP to be assigned (~5 minutes)
# Configure DNS: api.banking.yourcompany.com -> EXTERNAL-IP

# Test (after DNS propagation)
curl https://api.banking.yourcompany.com/health
```

---

## Phase 7: Monitoring Setup (15 minutes)

### Step 7.1: Verify Application Insights
```bash
# Check Application Insights in Azure Portal
az monitor app-insights component show \
  --resource-group $RESOURCE_GROUP \
  --app banking-ai-$ENVIRONMENT

# Expected: Data should start flowing within 5 minutes
```

### Step 7.2: Create Basic Alerts
```bash
# CPU alert
az monitor metrics alert create \
  --name "Banking API High CPU" \
  --resource-group $RESOURCE_GROUP \
  --scopes $(az aks show -g $RESOURCE_GROUP -n $AKS_NAME --query id -o tsv) \
  --condition "avg node_cpu_usage_percentage > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2

# Memory alert
az monitor metrics alert create \
  --name "Banking API High Memory" \
  --resource-group $RESOURCE_GROUP \
  --scopes $(az aks show -g $RESOURCE_GROUP -n $AKS_NAME --query id -o tsv) \
  --condition "avg node_memory_rss_percentage > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2
```

### Step 7.3: Create Log Analytics Queries
```bash
# Save useful queries in Log Analytics workspace
# - Application errors
# - Slow queries
# - Failed authentication attempts
# - API latency percentiles
```

---

## Phase 8: Load Testing (30 minutes - Optional)

### Step 8.1: Install k6
```bash
# macOS
brew install k6

# Linux
sudo apt-get install k6
```

### Step 8.2: Create Load Test Script
```javascript
// loadtest.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 10 },   // Ramp up
    { duration: '5m', target: 50 },   // Sustained load
    { duration: '2m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests < 500ms
    http_req_failed: ['rate<0.01'],   // <1% errors
  },
};

export default function () {
  let response = http.get('https://api.banking.yourcompany.com/health');
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
  
  sleep(1);
}
```

### Step 8.3: Run Load Test
```bash
k6 run loadtest.js
```

---

## Post-Deployment Checklist

- [ ] All pods running (3/3)
- [ ] Health endpoints responding
- [ ] Secrets mounted correctly
- [ ] Database connectivity working
- [ ] Application Insights receiving data
- [ ] Logs flowing to Log Analytics
- [ ] External DNS configured
- [ ] TLS certificate issued
- [ ] Alerts configured
- [ ] Load test passed
- [ ] Backup verified
- [ ] Documentation updated

---

## Troubleshooting

### Pods Not Starting

```bash
# Describe pod
kubectl describe pod -n banking <pod-name>

# Common issues:
# - ImagePullBackOff: Check ACR permissions
# - CrashLoopBackOff: Check application logs
# - Pending: Check resource quotas
```

### Secrets Not Mounted

```bash
# Check SecretProviderClass
kubectl describe secretproviderclass -n banking azure-banking-secrets

# Check pod events
kubectl get events -n banking --sort-by='.lastTimestamp'

# Verify Key Vault permissions
az keyvault show --name $KV_NAME
```

### Database Connection Issues

```bash
# Test from pod
kubectl exec -it -n banking <pod-name> -- /bin/sh
# Inside pod:
curl telnet://$SQL_SERVER:1433

# Check firewall rules
az sql server firewall-rule list \
  --resource-group $RESOURCE_GROUP \
  --server ${SQL_SERVER%%.*}
```

---

## Rollback Procedure

### Quick Rollback
```bash
# Rollback to previous version
kubectl rollout undo deployment/banking-api -n banking

# Check rollout status
kubectl rollout status deployment/banking-api -n banking
```

### Full Rollback
```bash
# Scale down
kubectl scale deployment/banking-api -n banking --replicas=0

# Delete all resources
kubectl delete namespace banking

# Redeploy previous version
# Follow deployment steps with previous image tag
```

---

## Support Contacts

- **Platform Team**: platform-team@yourcompany.com
- **On-Call**: See PagerDuty rotation
- **Slack**: #banking-service-support
- **Incident Management**: https://incidents.yourcompany.com

---

## Next Steps

1. **Enable monitoring dashboards** in Grafana
2. **Configure backup policies**
3. **Set up DR environment**
4. **Conduct security scan**
5. **Schedule load testing**
6. **Plan production release**

---

**Document Owner:** Platform Engineering Team  
**Last Review:** January 16, 2026  
**Next Review:** February 16, 2026
