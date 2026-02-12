# System Instructions for GitHub Copilot

This document provides context and instructions for AI assistants (GitHub Copilot, Cursor, etc.) working on this Banking Service project.

## Project Overview

This is a production-grade RESTful banking service built with:
- **.NET 8.0** - Modern C# web framework
- **Entity Framework Core** - ORM with dual database support
  - **SQLite** for local development (Data Source=banking.db)
  - **SQL Server** for production (Azure SQL Database)
  - Automatic switching based on ASPNETCORE_ENVIRONMENT
- **JWT Authentication** - Secure token-based auth
- **Serilog** - Structured logging to Application Insights
- **OpenTelemetry** - Distributed tracing
- **xUnit** - Testing framework
- **Docker** - Multi-stage containerization
- **Kubernetes** - Azure AKS orchestration

## Database Configuration

### Dual Database Setup (IMPORTANT)

The application uses different databases based on environment:

```csharp
// In Program.cs - Automatic environment detection
if (builder.Environment.IsDevelopment())
{
    // Local development - SQLite (zero setup)
    options.UseSqlite(connectionString);
}
else
{
    // Production - SQL Server (Azure SQL)
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, ...);
        sqlOptions.CommandTimeout(30);
    });
}
```

**Development:**
- Database: SQLite
- Connection String: `Data Source=banking.db`
- Location: Project root (`banking.db` file)
- Setup: Automatic (no installation needed)

**Production:**
- Database: Azure SQL Server
- Connection String: From Azure Key Vault
- Setup: Configured via Kubernetes secrets

**Why Both?**
- Developers get zero-setup experience
- Production gets enterprise-grade database
- Same code works in both environments

## Architecture Principles

### 1. Clean Architecture
- **Controllers**: Handle HTTP requests/responses only
- **Services**: Contain all business logic
- **Models**: Domain entities (database models)
- **DTOs**: Data transfer objects for API contracts
- **Middleware**: Cross-cutting concerns

### 2. Service Layer Pattern
All business logic MUST be in service classes implementing interfaces:
```csharp
public interface IMyService
{
    Task<ResultDto> DoSomethingAsync(RequestDto request);
}

public class MyService : IMyService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<MyService> _logger;
    
    // Implementation
}
```

### 3. Dependency Injection
- Register all services in `Program.cs`
- Use constructor injection
- Inject interfaces, not concrete classes

## Code Generation Guidelines

### When Creating Controllers

1. **Always inherit from ControllerBase**
2. **Apply [ApiController] and [Route] attributes**
3. **Add [Authorize] for protected endpoints**
4. **Use ApiResponse<T> wrapper for all responses**
5. **Extract userId from claims using GetUserId() helper**
6. **Handle exceptions and return appropriate status codes**

**Template:**
```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    private readonly IMyService _service;
    private readonly ILogger<MyController> _logger;

    public MyController(IMyService service, ILogger<MyController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResultDto>>> Create([FromBody] RequestDto request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _service.CreateAsync(userId, request);
            return Ok(new ApiResponse<ResultDto>
            {
                Success = true,
                Data = result,
                Message = "Created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating resource");
            return BadRequest(new ApiResponse<ResultDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
```

### When Creating Services

1. **Define interface first**
2. **Inject DbContext and ILogger**
3. **Use async/await for all database operations**
4. **Use transactions for multi-step operations**
5. **Log important operations with context**
6. **Throw meaningful exceptions**

**Template:**
```csharp
public interface IMyService
{
    Task<ResultDto> CreateAsync(Guid userId, RequestDto request);
    Task<List<ResultDto>> GetAllAsync(Guid userId);
}

public class MyService : IMyService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<MyService> _logger;

    public MyService(BankingDbContext context, ILogger<MyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultDto> CreateAsync(Guid userId, RequestDto request)
    {
        _logger.LogInformation("Creating resource for user: {UserId}", userId);

        // Validate user ownership
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Business logic here
        var entity = new MyEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            // Map properties
        };

        _context.MyEntities.Add(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource created: {EntityId}", entity.Id);

        return MapToDto(entity);
    }
}
```

### When Creating Models

1. **Use Guid for primary keys**
2. **Add proper attributes ([Required], [MaxLength], etc.)**
3. **Use [ForeignKey] for relationships**
4. **Add navigation properties (virtual)**
5. **Set default values where appropriate**
6. **Use enums for status/type fields**

**Template:**
```csharp
public class MyEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    
    public MyStatus Status { get; set; } = MyStatus.Active;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<RelatedEntity> RelatedEntities { get; set; } = new List<RelatedEntity>();
}

public enum MyStatus
{
    Active,
    Inactive,
    Suspended
}
```

### When Creating DTOs

1. **Use DataAnnotations for validation**
2. **Keep DTOs flat (no nested objects unless necessary)**
3. **Separate Request and Response DTOs**
4. **No business logic in DTOs**

**Template:**
```csharp
public class CreateMyEntityRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
}

public class MyEntityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public MyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### When Writing Tests

1. **Use descriptive test names: MethodName_Scenario_ExpectedResult**
2. **Follow AAA pattern: Arrange, Act, Assert**
3. **Use in-memory SQLite database for tests (fast!)**
4. **Mock dependencies for unit tests**
5. **Test both success and failure scenarios**
6. **Implement IDisposable to clean up test data**

**Unit Test Template:**
```csharp
public class MyServiceTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly MyService _service;
    private readonly User _testUser;

    public MyServiceTests()
    {
        // Use in-memory SQLite for fast tests
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();
        
        var logger = new Mock<ILogger<MyService>>().Object;
        _service = new MyService(_context, logger);

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateEntity_ValidRequest_ReturnsDto()
    {
        // Arrange
        var request = new CreateMyEntityRequest
        {
            Name = "Test Entity",
            Amount = 100
        };

        // Act
        var result = await _service.CreateAsync(_testUser.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Entity", result.Name);
        Assert.Equal(100, result.Amount);
    }

    [Fact]
    public async Task CreateEntity_InvalidUser_ThrowsException()
    {
        // Arrange
        var request = new CreateMyEntityRequest
        {
            Name = "Test Entity",
            Amount = 100
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.CreateAsync(Guid.NewGuid(), request));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

**Integration Test Template:**
```csharp
public class MyControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public MyControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateEntity_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        var request = new CreateMyEntityRequest
        {
            Name = "Test",
            Amount = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/myentities", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<MyEntityDto>>();
        Assert.True(result!.Success);
    }
}
```

## Database Guidelines

### DbContext Configuration
When adding new entities to DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<MyEntity>(entity =>
    {
        // Indexes
        entity.HasIndex(e => e.Name).IsUnique();
        
        // String lengths
        entity.Property(e => e.Name).HasMaxLength(200);
        
        // Relationships
        entity.HasOne(e => e.User)
            .WithMany(u => u.MyEntities)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    });
}
```

### Migration Management

**IMPORTANT:** Use migrations for ALL schema changes:

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

**DO NOT use:**
```csharp
db.Database.EnsureCreated();  // ❌ Bad for production
```

**DO use:**
```csharp
await db.Database.MigrateAsync();  // ✅ Proper migrations
```

### Transaction Management
Use transactions for operations that modify multiple entities:

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Multiple operations
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Logging Guidelines

### Log to Application Insights (Production)

```csharp
// Serilog configuration in Program.cs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        builder.Configuration["ApplicationInsights:ConnectionString"],
        TelemetryConverter.Traces)
    .CreateLogger();
```

### Log Levels
- **Information**: Important business events (created, updated, deleted)
- **Warning**: Unexpected but handled situations (validation failures)
- **Error**: Exceptions and failures (database errors, external API failures)

### Log Format
```csharp
_logger.LogInformation("Entity created: {EntityId}, User: {UserId}", entityId, userId);
_logger.LogWarning("Validation failed for user: {UserId}, Reason: {Reason}", userId, reason);
_logger.LogError(ex, "Failed to process transaction: {TransactionId}", transactionId);
```

## Security Guidelines

1. **Always validate user ownership**
   ```csharp
   var entity = await _context.MyEntities
       .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
   if (entity == null)
       throw new InvalidOperationException("Not found or unauthorized");
   ```

2. **Never expose sensitive data**
   - Hash passwords
   - Mask card numbers
   - Don't log passwords or tokens

3. **Validate all input**
   - Use DataAnnotations
   - Additional business validation in services

4. **Use parameterized queries** (EF Core handles this)

5. **Secrets from Azure Key Vault**
   - Never hardcode connection strings
   - Never hardcode JWT keys
   - Load from configuration/Key Vault

## Error Handling

### Service Layer
```csharp
if (condition)
    throw new InvalidOperationException("Clear error message");
```

### Controller Layer
```csharp
try
{
    // Service call
}
catch (UnauthorizedAccessException ex)
{
    return Unauthorized(new ApiResponse<T> { Success = false, Message = ex.Message });
}
catch (InvalidOperationException ex)
{
    return BadRequest(new ApiResponse<T> { Success = false, Message = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, new ApiResponse<T> { Success = false, Message = "Internal server error" });
}
```

## Naming Conventions

- **Controllers**: `[EntityName]Controller` (e.g., `AccountsController`)
- **Services**: `I[EntityName]Service`, `[EntityName]Service`
- **Models**: Singular nouns (e.g., `Account`, not `Accounts`)
- **DTOs**: `[Action][EntityName]Request/Response` or `[EntityName]Dto`
- **Methods**: Async methods end with `Async`
- **Private fields**: `_camelCase`
- **Properties**: `PascalCase`

## Testing Requirements

For every new feature, generate:
1. **Unit tests** for service methods (at least 2 per method: success + failure)
2. **Integration tests** for API endpoints
3. **Edge case tests** for business logic

Target: 80%+ code coverage

Use **in-memory SQLite** for fast tests:
```csharp
var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();
var options = new DbContextOptionsBuilder<BankingDbContext>()
    .UseSqlite(connection)
    .Options;
```

## Performance Guidelines

1. **Use async/await consistently**
2. **Eager load related data when needed**
   ```csharp
   .Include(e => e.RelatedEntity)
   ```
3. **Project only needed fields**
   ```csharp
   .Select(e => new MyDto { ... })
   ```
4. **Add indexes for frequently queried fields**

## Response Format

All API responses MUST use the standard wrapper:
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
```

## Common Patterns

### Mapping Entities to DTOs
```csharp
private MyEntityDto MapToDto(MyEntity entity)
{
    return new MyEntityDto
    {
        Id = entity.Id,
        Name = entity.Name,
        // ... other properties
    };
}
```

### Generating Unique Identifiers
```csharp
private string GenerateUniqueNumber()
{
    return $"PREFIX{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
}
```

## Environment-Specific Behavior

### Development
```bash
ASPNETCORE_ENVIRONMENT=Development
```
- Uses SQLite database
- Detailed error messages
- Swagger enabled
- Sensitive data logging enabled

### Production
```bash
ASPNETCORE_ENVIRONMENT=Production
```
- Uses SQL Server (Azure SQL)
- Minimal logging
- Swagger disabled
- No sensitive data in logs
- Secrets from Key Vault

## Questions to Ask Before Generating Code

1. Does this require authentication? (Add [Authorize])
2. Does this modify data? (Use transactions if multiple entities)
3. Does this need to verify user ownership?
4. What validation rules apply?
5. What should be logged?
6. What exceptions can occur?
7. What tests are needed?
8. Will this work with both SQLite and SQL Server?

## File Organization

```
BankingService/
├── Controllers/
│   └── [EntityName]Controller.cs
├── Services/
│   ├── I[EntityName]Service.cs
│   └── [EntityName]Service.cs
├── Models/
│   └── [EntityName].cs
├── DTOs/
│   └── [EntityName]Dtos.cs
├── Data/
│   ├── BankingDbContext.cs
│   └── Migrations/
└── Middleware/
    └── [Middleware]Middleware.cs
```

## Remember

- **Security first**: Always validate user ownership and input
- **Consistency**: Follow existing patterns in the codebase
- **Testability**: Write testable code with clear dependencies
- **Logging**: Log important events with context
- **Documentation**: Update README.md when adding features
- **Error handling**: Graceful degradation and clear error messages
- **Database compatibility**: Ensure code works with both SQLite and SQL Server
- **Environment awareness**: Use builder.Environment.IsDevelopment() for env-specific behavior
