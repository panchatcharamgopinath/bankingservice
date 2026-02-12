# Database Migration Guide: SQLite to SQL Server

## Overview

This guide explains how to migrate from SQLite (development) to Azure SQL Server (production).

---

## Prerequisites

- .NET 8.0 SDK installed
- SQL Server (LocalDB for dev, Azure SQL for production)
- Entity Framework Core CLI tools

### Install EF Core Tools
```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

---

## Step 1: Verify Package Installation

Check that SQL Server packages are installed:

```bash
dotnet list package | grep SqlServer
```

Expected output:
```
Microsoft.EntityFrameworkCore.SqlServer    8.0.0
```

---

## Step 2: Create Initial Migration

```bash
cd src

# Create migration
dotnet ef migrations add InitialCreate \
  --output-dir Data/Migrations \
  --verbose

# Verify migration was created
ls Data/Migrations/
```

Expected files:
- `20XXXXXX_InitialCreate.cs`
- `20XXXXXX_InitialCreate.Designer.cs`
- `BankingDbContextModelSnapshot.cs`

---

## Step 3: Update Development Database (LocalDB)

### Connection String (appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BankingDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Apply Migration:
```bash
dotnet ef database update --verbose
```

### Verify Database:
```bash
# Connect using SSMS or Azure Data Studio
Server: (localdb)\mssqllocaldb
Database: BankingDB

# Or use command line
sqlcmd -S "(localdb)\mssqllocaldb" -d BankingDB -Q "SELECT name FROM sys.tables"
```

Expected tables:
- Users
- Accounts
- Cards
- Transactions
- __EFMigrationsHistory

---

## Step 4: Generate SQL Script (for review)

```bash
# Generate idempotent script
dotnet ef migrations script \
  --idempotent \
  --output migration.sql

# Review the script
cat migration.sql
```

This script can be reviewed by DBAs before production deployment.

---

## Step 5: Setup Azure SQL Database

### Option A: Using Azure Portal

1. Create Azure SQL Server:
   ```
   Name: banking-sql-prod
   Region: East US
   Authentication: SQL Authentication
   Admin: sqladmin
   Password: [Strong Password]
   ```

2. Create Database:
   ```
   Name: BankingDB
   Pricing: Standard S3 (100 DTU)
   Backup: Geo-redundant
   ```

3. Configure Firewall:
   - Add your IP for initial setup
   - Add Azure Services (0.0.0.0)
   - Add AKS subnet range

### Option B: Using Azure CLI

```bash
# Variables
RESOURCE_GROUP="banking-rg-prod"
SQL_SERVER="banking-sql-prod"
DB_NAME="BankingDB"
ADMIN_USER="sqladmin"
ADMIN_PASSWORD="YourStrongPassword123!"

# Create SQL Server
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location eastus \
  --admin-user $ADMIN_USER \
  --admin-password $ADMIN_PASSWORD

# Create Database
az sql db create \
  --name $DB_NAME \
  --server $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --service-objective S3 \
  --backup-storage-redundancy Geo

# Configure Firewall
az sql server firewall-rule create \
  --server $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

---

## Step 6: Deploy to Azure SQL

### Get Connection String:
```bash
az sql db show-connection-string \
  --server $SQL_SERVER \
  --name $DB_NAME \
  --client ado.net
```

Example output:
```
Server=tcp:banking-sql-prod.database.windows.net,1433;
Database=BankingDB;
User ID=sqladmin;
Password={your_password};
Encrypt=true;
TrustServerCertificate=false;
Connection Timeout=30;
```

### Store in Azure Key Vault:
```bash
KV_NAME="banking-kv-prod"

az keyvault secret set \
  --vault-name $KV_NAME \
  --name banking-sql-connection-string \
  --value "Server=tcp:banking-sql-prod.database.windows.net,1433;Database=BankingDB;User ID=sqladmin;Password=YourPassword;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
```

### Apply Migration to Azure SQL:

**Method 1: From CI/CD Pipeline**
```bash
# Set connection string
export ConnectionStrings__DefaultConnection="Server=tcp:..."

# Run migration
dotnet ef database update --project src
```

**Method 2: Using SQL Script**
```bash
# Upload and execute migration.sql via Azure Portal or SSMS
sqlcmd -S banking-sql-prod.database.windows.net \
  -U sqladmin \
  -P YourPassword \
  -d BankingDB \
  -i migration.sql
```

**Method 3: From Application Startup** (already configured)
```csharp
// In Program.cs - this runs automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
    await db.Database.MigrateAsync();  // Applies pending migrations
    Log.Information("Database initialized successfully");
}
```

---

## Step 7: Verify Migration

```sql
-- Connect to Azure SQL Database
-- Check migrations history
SELECT * FROM __EFMigrationsHistory;

-- Verify tables
SELECT name FROM sys.tables ORDER BY name;

-- Check table structures
EXEC sp_help 'Users';
EXEC sp_help 'Accounts';
EXEC sp_help 'Transactions';

-- Verify indexes
SELECT 
    i.name as IndexName,
    t.name as TableName,
    c.name as ColumnName
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.is_primary_key = 0
ORDER BY t.name, i.name;
```

---

## Step 8: Data Migration (if needed)

If you have existing data in SQLite to migrate:

### Export from SQLite:
```bash
# Install sqlite3
sqlite3 banking.db

# Export data
.mode insert Users
.output users.sql
SELECT * FROM Users;

.mode insert Accounts
.output accounts.sql
SELECT * FROM Accounts;

# Repeat for other tables
.quit
```

### Import to SQL Server:
```bash
# Convert INSERT statements to SQL Server format
# Then execute via sqlcmd or SSMS
sqlcmd -S banking-sql-prod.database.windows.net \
  -U sqladmin \
  -P YourPassword \
  -d BankingDB \
  -i users.sql
```

**Note**: For production, consider using Azure Data Migration Service for large datasets.

---

## Step 9: Update Application Configuration

### Development (appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BankingDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Production (from Key Vault):
- Connection string loaded from `banking-sql-connection-string` secret
- Configured in Kubernetes SecretProviderClass
- No secrets in application code

---

## Step 10: Test Database Connectivity

### From Application:
```bash
# Run health check
dotnet run --project src
curl http://localhost:8080/health

# Expected response:
{
  "status": "Healthy",
  "checks": {
    "database": {
      "status": "Healthy"
    }
  }
}
```

### From Code (integration test):
```csharp
[Fact]
public async Task DatabaseConnection_Should_BeHealthy()
{
    var context = _factory.Services.GetRequiredService<BankingDbContext>();
    var canConnect = await context.Database.CanConnectAsync();
    Assert.True(canConnect);
}
```

---

## Common Issues & Solutions

### Issue 1: Migration Already Applied
```
Error: The migration 'InitialCreate' has already been applied to the database
```

**Solution:**
```bash
# Check migration status
dotnet ef migrations list

# If needed, rollback and reapply
dotnet ef database update 0  # Rollback all
dotnet ef database update    # Apply all
```

### Issue 2: Connection Timeout
```
Error: A network-related or instance-specific error occurred
```

**Solution:**
1. Check firewall rules in Azure SQL
2. Verify connection string is correct
3. Ensure SQL Server is running
4. Check network connectivity

### Issue 3: Authentication Failed
```
Error: Login failed for user 'sqladmin'
```

**Solution:**
1. Verify username and password
2. Check if user exists in SQL Server
3. Ensure connection string has correct credentials

### Issue 4: Cannot Drop Database
```
Error: Cannot drop database because it is currently in use
```

**Solution:**
```sql
-- Set database to single user mode
ALTER DATABASE BankingDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE BankingDB;
```

---

## Migration Rollback

### Rollback Last Migration:
```bash
# Remove migration from database
dotnet ef database update PreviousMigrationName

# Delete migration files
dotnet ef migrations remove
```

### Rollback All Migrations:
```bash
# Rollback to empty database
dotnet ef database update 0

# Drop and recreate
dotnet ef database drop
dotnet ef database update
```

---

## Performance Optimization

### Add Indexes (if not in migration):
```sql
CREATE NONCLUSTERED INDEX IX_Accounts_UserId_Status 
ON Accounts(UserId, Status) 
INCLUDE (Balance, AccountType);

CREATE NONCLUSTERED INDEX IX_Transactions_AccountId_Date 
ON Transactions(FromAccountId, CreatedAt DESC) 
INCLUDE (Amount, TransactionType);
```

### Enable Query Store:
```sql
ALTER DATABASE BankingDB SET QUERY_STORE = ON;
```

### Monitor Performance:
```sql
-- Expensive queries
SELECT TOP 10 
    SUBSTRING(qt.TEXT, (qs.statement_start_offset/2)+1,
    ((CASE qs.statement_end_offset
    WHEN -1 THEN DATALENGTH(qt.TEXT)
    ELSE qs.statement_end_offset
    END - qs.statement_start_offset)/2)+1),
    qs.execution_count,
    qs.total_logical_reads,
    qs.total_worker_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY qs.total_logical_reads DESC;
```

---

## Backup & Recovery

### Configure Automated Backups:
```bash
# Set backup retention (7-35 days)
az sql db update \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $DB_NAME \
  --backup-storage-redundancy Geo \
  --retention-days 30
```

### Manual Backup:
```bash
# Create backup
az sql db copy \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $DB_NAME \
  --dest-name BankingDB_Backup_$(date +%Y%m%d)
```

### Point-in-Time Restore:
```bash
# Restore to specific time
az sql db restore \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $DB_NAME \
  --dest-name BankingDB_Restored \
  --time "2024-01-17T10:00:00Z"
```

---

## Monitoring

### Enable Diagnostics:
```bash
LOG_ANALYTICS_ID="/subscriptions/.../workspaces/banking-log-prod"

az monitor diagnostic-settings create \
  --resource $(az sql db show --resource-group $RESOURCE_GROUP --server $SQL_SERVER --name $DB_NAME --query id -o tsv) \
  --name sql-diagnostics \
  --workspace $LOG_ANALYTICS_ID \
  --logs '[{"category":"SQLInsights","enabled":true},{"category":"Errors","enabled":true}]' \
  --metrics '[{"category":"AllMetrics","enabled":true}]'
```

### Alert on High DTU:
```bash
az monitor metrics alert create \
  --name "High DTU Usage" \
  --resource-group $RESOURCE_GROUP \
  --scopes $(az sql db show --resource-group $RESOURCE_GROUP --server $SQL_SERVER --name $DB_NAME --query id -o tsv) \
  --condition "avg dtu_consumption_percent > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2
```

---

## Best Practices

1. **Always use migrations** - Never use `EnsureCreated()` in production
2. **Test migrations locally** before applying to production
3. **Generate SQL scripts** for DBA review
4. **Use transactions** for complex migrations
5. **Take backups** before major migrations
6. **Monitor migration time** - Long-running migrations can cause downtime
7. **Plan maintenance windows** for production deployments
8. **Keep migration files** in source control
9. **Document schema changes** in migration comments
10. **Test rollback procedures** before production deployment

---

## Checklist

- [ ] SQL Server packages installed
- [ ] Initial migration created
- [ ] LocalDB updated successfully
- [ ] Azure SQL Database created
- [ ] Firewall rules configured
- [ ] Connection string in Key Vault
- [ ] Migration applied to Azure SQL
- [ ] Tables and indexes verified
- [ ] Application connects successfully
- [ ] Health check passes
- [ ] Backups configured
- [ ] Monitoring enabled
- [ ] Documentation updated

---

**Migration Complete!** Your Banking Service is now running on SQL Server. 🎉
