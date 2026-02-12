# Dual Database Setup: SQLite (Dev) + SQL Server (Production)

## Overview

The Banking Service is configured to use:
- **SQLite** for local development (easy setup, no external dependencies)
- **SQL Server** for production (scalable, enterprise-grade, Azure-ready)

This setup allows developers to work locally without needing SQL Server while ensuring production uses a robust database.

---

## How It Works

### Automatic Environment Detection

The application automatically chooses the database provider based on the environment:

```csharp
// In Program.cs
if (builder.Environment.IsDevelopment())
{
    // Use SQLite for local development
    options.UseSqlite(connectionString);
}
else
{
    // Use SQL Server for production
    options.UseSqlServer(connectionString, sqlOptions => { ... });
}
```

### Environment Detection

The environment is determined by the `ASPNETCORE_ENVIRONMENT` variable:

- **Development**: `ASPNETCORE_ENVIRONMENT=Development`
- **Production**: `ASPNETCORE_ENVIRONMENT=Production`
- **Staging**: `ASPNETCORE_ENVIRONMENT=Staging`

---

## Configuration Files

### Development (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=banking.db"
  }
}
```

This creates a SQLite database file `banking.db` in your project directory.

### Production (appsettings.Production.json)

Connection string comes from Azure Key Vault (no hardcoded values):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:banking-sql-prod.database.windows.net,1433;Database=BankingDB;User ID=sqladmin;Password={from-keyvault};Encrypt=true;"
  }
}
```

---

## Development Setup (SQLite)

### 1. No Installation Required

SQLite is embedded - no separate installation needed!

### 2. Create Database

```bash
cd src

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration (creates banking.db)
dotnet ef database update
```

### 3. Run Application

```bash
dotnet run
```

The database file `banking.db` will be created automatically in the `src/` directory.

### 4. View SQLite Database

**Option A: Visual Studio Code**
- Install extension: "SQLite Viewer"
- Right-click `banking.db` → Open Database

**Option B: DB Browser for SQLite**
```bash
# Download from https://sqlitebrowser.org/
# Open banking.db file
```

**Option C: Command Line**
```bash
sqlite3 banking.db

# SQLite commands
.tables          # List all tables
.schema Users    # View table schema
SELECT * FROM Users;
.quit
```

### 5. Reset Development Database

```bash
# Delete the database file
rm banking.db

# Recreate it
dotnet ef database update
```

---

## Production Setup (SQL Server)

### 1. Azure SQL Database

Already configured in your Bicep templates (infra/modules/sql.bicep):

```bash
# Create Azure SQL Database
az deployment group create \
  --resource-group banking-rg-prod \
  --template-file infra/main.bicep \
  --parameters environment=prod
```

### 2. Apply Migrations

**Option A: From CI/CD Pipeline**
```bash
# Set environment to Production
export ASPNETCORE_ENVIRONMENT=Production

# Connection string from Key Vault
export ConnectionStrings__DefaultConnection="Server=tcp:..."

# Run migrations
dotnet ef database update
```

**Option B: Automatic on Startup**

The application automatically runs migrations on startup:

```csharp
// In Program.cs - already configured
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
    await db.Database.MigrateAsync();  // Applies pending migrations
}
```

**Option C: Manual SQL Script**
```bash
# Generate SQL script
dotnet ef migrations script --output production-migration.sql

# Review and apply via Azure Portal or sqlcmd
sqlcmd -S banking-sql-prod.database.windows.net \
  -U sqladmin -P {password} \
  -d BankingDB \
  -i production-migration.sql
```

---

## Package Requirements

Both database providers are included:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
```

---

## Migration Strategy

### Single Migration for Both Databases

Entity Framework creates migrations that work with both SQLite and SQL Server:

```bash
# Create migration
dotnet ef migrations add YourMigrationName

# This creates files in Data/Migrations/
# They work with BOTH SQLite and SQL Server!
```

### Differences Handled Automatically

EF Core handles differences between databases:

| Feature | SQLite | SQL Server | Auto-Handled |
|---------|--------|------------|--------------|
| Auto-increment | `AUTOINCREMENT` | `IDENTITY` | ✅ Yes |
| Dates | `TEXT` | `datetime2` | ✅ Yes |
| Decimals | `TEXT` | `decimal(18,2)` | ✅ Yes |
| Default values | `CURRENT_TIMESTAMP` | `GETUTCDATE()` | ✅ Yes |

---

## Testing Different Environments

### Test as Development (SQLite)

```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run

# Uses banking.db (SQLite)
```

### Test as Production (SQL Server)

```bash
# Setup local SQL Server or Azure SQL
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=localhost;Database=BankingDB;..."
dotnet run

# Uses SQL Server
```

---

## Docker Configuration

### Development Container (SQLite)

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Development
# SQLite database file persisted in volume
```

### Production Container (SQL Server)

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
# Connection string from Azure Key Vault
```

---

## Kubernetes Configuration

### Development/Staging

```yaml
env:
- name: ASPNETCORE_ENVIRONMENT
  value: "Development"  # Uses SQLite
```

### Production

```yaml
env:
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"  # Uses SQL Server
- name: ConnectionStrings__DefaultConnection
  valueFrom:
    secretKeyRef:
      name: banking-secrets
      key: connection-string  # From Azure Key Vault
```

---

## Common Scenarios

### Scenario 1: Local Development

```bash
# Just run it - SQLite works out of the box
dotnet run

# Database file: src/banking.db
```

### Scenario 2: Team Collaboration

```bash
# Add .gitignore entry
echo "banking.db" >> .gitignore
echo "banking.db-shm" >> .gitignore
echo "banking.db-wal" >> .gitignore

# Each developer gets their own local database
# No conflicts!
```

### Scenario 3: Integration Testing

```csharp
public class IntegrationTests
{
    [Fact]
    public async Task TestWithInMemoryDatabase()
    {
        // Use in-memory SQLite for fast tests
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(connection)
            .Options;
            
        using var context = new BankingDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        // Run tests...
    }
}
```

### Scenario 4: Production Deployment

```bash
# Environment variable set by Kubernetes
ASPNETCORE_ENVIRONMENT=Production

# Connection string from Azure Key Vault (automatic)
# Application uses SQL Server
```

---

## Advantages of This Setup

### For Developers

✅ **No SQL Server installation required**
- SQLite works immediately
- No configuration needed
- Cross-platform (Windows, Mac, Linux)

✅ **Fast iteration**
- Delete database file to reset
- No connection issues
- Lightweight and fast

✅ **Easy debugging**
- View database with simple tools
- Database file in project directory

### For Production

✅ **Enterprise-grade database**
- Azure SQL Database
- High availability
- Automatic backups
- Geo-replication

✅ **Scalability**
- Handle millions of transactions
- Multiple concurrent connections
- Advanced query optimization

✅ **Security**
- Encryption at rest and in transit
- Advanced threat protection
- Audit logging

---

## Troubleshooting

### Issue: "SQLite Error 1: 'no such table'"

**Solution**: Create migrations and update database
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Issue: "Unable to open database file"

**Solution**: Check file permissions
```bash
chmod 666 banking.db
```

### Issue: "Different database in production"

**Solution**: Verify environment variable
```bash
echo $ASPNETCORE_ENVIRONMENT
# Should be "Production" for SQL Server
```

### Issue: "Migration works in SQLite but fails in SQL Server"

**Solution**: Test migration against both databases
```bash
# Test with SQLite
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update

# Test with SQL Server
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update
```

---

## Best Practices

### 1. Keep Database File Out of Git

```gitignore
# .gitignore
*.db
*.db-shm
*.db-wal
```

### 2. Use Migrations for Schema Changes

```bash
# ALWAYS use migrations, never modify database directly
dotnet ef migrations add AddNewField
dotnet ef database update
```

### 3. Test Migrations on Both Databases

Before deploying to production, test migrations work on SQL Server:

```bash
# Local SQL Server testing
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=localhost;Database=BankingDB;User ID=sa;Password=YourPassword123!;TrustServerCertificate=True"
dotnet ef database update
```

### 4. Use Same Migration Files

Don't create separate migrations for SQLite and SQL Server - use the same migration files for both.

### 5. Document Environment Variables

```bash
# Development
export ASPNETCORE_ENVIRONMENT=Development

# Production  
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="{from-azure-keyvault}"
```

---

## Quick Reference

### Local Development Commands

```bash
# Create database
dotnet ef database update

# View database
sqlite3 banking.db

# Reset database
rm banking.db && dotnet ef database update

# Add new migration
dotnet ef migrations add MigrationName
```

### Production Commands

```bash
# Generate SQL script for review
dotnet ef migrations script -o migration.sql

# Apply migration (automatic on app startup)
# OR manually via Azure Portal/sqlcmd
```

### Check Current Environment

```bash
# In code
builder.Environment.IsDevelopment()  // true in dev
builder.Environment.IsProduction()   // true in prod

# Command line
echo $ASPNETCORE_ENVIRONMENT
```

---

## Summary

This dual-database approach gives you the best of both worlds:

🎯 **Development**: Fast, simple, no setup (SQLite)  
🎯 **Production**: Robust, scalable, enterprise-grade (SQL Server)  
🎯 **Same Code**: Automatic switching based on environment  
🎯 **Same Migrations**: One set of migrations for both databases  

You can develop locally without any database setup, then deploy to production with confidence that it will work with Azure SQL Database!

---

**Last Updated**: January 17, 2026  
**Status**: Production Ready with Dual Database Support ✅
