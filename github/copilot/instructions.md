# GitHub Copilot Instructions for Banking Service

## Project Overview
This is a production-grade Banking Service API built with .NET 8.0, targeting enterprise deployment on Azure Kubernetes Service (AKS).

## Architecture
- **Framework**: ASP.NET Core 8.0 Web API
- **Database**: Azure SQL Server (Production), LocalDB (Development)
- **Authentication**: JWT Bearer tokens
- **Logging**: Serilog → Azure Application Insights
- **Monitoring**: OpenTelemetry with Azure Monitor
- **Containerization**: Docker multi-stage builds
- **Orchestration**: Kubernetes on Azure AKS

## Code Standards

### Naming Conventions
- **Classes/Interfaces**: PascalCase (e.g., `IAccountService`, `TransactionController`)
- **Methods**: PascalCase (e.g., `GetAccountById`, `CreateTransaction`)
- **Variables**: camelCase (e.g., `accountId`, `transactionAmount`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_TRANSFER_AMOUNT`)
- **Private fields**: _camelCase (e.g., `_dbContext`, `_logger`)

### Project Structure
```
src/
├── Controllers/        # API endpoints
├── Services/          # Business logic layer
├── Data/              # EF Core DbContext and migrations
├── Models/            # Domain entities
├── Dtos/              # Data transfer objects
├── Middleware/        # Custom middleware components
└── Program.cs         # Application entry point
```

### Database Access
- Always use Entity Framework Core
- Use async/await for all database operations
- Implement proper error handling and transactions
- Use migrations for schema changes (not EnsureCreated)

```csharp
// Good
public async Task<Account> GetAccountAsync(int id)
{
    return await _context.Accounts
        .Include(a => a.User)
        .FirstOrDefaultAsync(a => a.Id == id);
}

// Bad - synchronous
public Account GetAccount(int id)
{
    return _context.Accounts.Find(id);
}
```

### Error Handling
- Use try-catch with specific exceptions
- Log all errors with appropriate severity
- Return meaningful error messages to clients
- Never expose sensitive information in errors

```csharp
try
{
    // Business logic
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database error while processing transaction {TransactionId}", id);
    throw new InvalidOperationException("Unable to process transaction", ex);
}
```

### Logging
- Use structured logging with Serilog
- Include correlation IDs for request tracking
- Log at appropriate levels: Debug, Information, Warning, Error, Critical
- Don't log sensitive data (passwords, PINs, full card numbers)

```csharp
_logger.LogInformation(
    "Transaction {TransactionId} created for account {AccountId} with amount {Amount}",
    transaction.Id,
    accountId,
    amount);
```

### Security Best Practices
- Never hardcode secrets or connection strings
- Use Azure Key Vault for production secrets
- Validate all inputs
- Implement rate limiting on public endpoints
- Use parameterized queries (EF Core handles this)
- Implement proper CORS policies

### API Design
- Follow RESTful conventions
- Use HTTP status codes correctly (200, 201, 400, 401, 404, 500)
- Version APIs (currently v1)
- Document all endpoints with XML comments
- Use DTOs for request/response objects (never expose entities directly)

```csharp
/// <summary>
/// Creates a new bank account for the authenticated user
/// </summary>
/// <param name="request">Account creation details</param>
/// <returns>Created account information</returns>
[HttpPost]
[ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountRequest request)
{
    // Implementation
}
```

### Testing
- Write unit tests for all service methods
- Use xUnit framework
- Mock external dependencies
- Test both success and failure scenarios
- Aim for >80% code coverage

```csharp
[Fact]
public async Task CreateAccount_ValidRequest_ReturnsAccount()
{
    // Arrange
    var mockContext = CreateMockDbContext();
    var service = new AccountService(mockContext, _logger);
    
    // Act
    var result = await service.CreateAccountAsync(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedAccountNumber, result.AccountNumber);
}
```

### Performance
- Use async/await consistently
- Implement caching where appropriate
- Use Select() to project only needed fields
- Paginate large result sets
- Use Include() carefully to avoid N+1 queries

### Docker & Kubernetes
- Use multi-stage builds
- Run as non-root user (UID 10001)
- Set resource limits (CPU, memory)
- Implement proper health checks (/health, /ready)
- Use secrets from Azure Key Vault (not ConfigMaps)

## Common Patterns

### Service Pattern
```csharp
public interface IAccountService
{
    Task<AccountDto> GetAccountByIdAsync(int id);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request);
    Task<bool> UpdateAccountAsync(int id, UpdateAccountRequest request);
    Task<bool> DeleteAccountAsync(int id);
}

public class AccountService : IAccountService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<AccountService> _logger;
    
    public AccountService(BankingDbContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // Implementation
}
```

### Controller Pattern
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;
    
    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetAccount(int id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account == null)
            return NotFound();
        return Ok(account);
    }
}
```

## Environment-Specific Configurations

### Development
- Use LocalDB or SQL Server Developer Edition
- Enable detailed errors and sensitive data logging
- Swagger UI enabled
- CORS allows localhost origins

### Production
- Azure SQL Database
- Minimal logging (Information and above)
- Swagger disabled
- CORS restricted to known domains
- All secrets from Azure Key Vault
- Rate limiting enabled
- Application Insights enabled

## Deployment Checklist
- [ ] All secrets removed from code
- [ ] Database migrations created and tested
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Docker image builds successfully
- [ ] Kubernetes manifests validated
- [ ] Health checks responding
- [ ] Application Insights configured
- [ ] Resource limits set appropriately
- [ ] Security scanning complete

## Useful Commands

### Build & Run
```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run locally
dotnet run --project src/BankingService.csproj

# Run tests
dotnet test
```

### Database Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName --project src

# Update database
dotnet ef database update --project src

# Generate SQL script
dotnet ef migrations script --project src
```

### Docker
```bash
# Build image
docker build -f docker/Dockerfile -t banking-api:latest .

# Run container
docker run -p 8080:8080 banking-api:latest

# Test health
curl http://localhost:8080/health
```

## Contact & Support
- **Team**: Platform Engineering
- **Slack**: #banking-service-dev
- **Documentation**: /docs folder
