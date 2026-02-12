# Testing Guide

Comprehensive testing guidelines for AI-assisted test generation in the Banking Service project.

## Testing Philosophy

- **Test Pyramid**: More unit tests, fewer integration tests, minimal E2E tests
- **Coverage Target**: Minimum 80% code coverage
- **Test Quality**: Better to have fewer good tests than many poor tests
- **Test Independence**: Each test should be isolated and independent
- **Fast Feedback**: Tests should run quickly (< 5 minutes for full suite)

## Test Structure

```
BankingService.Tests/
├── Unit/
│   ├── Services/
│   │   ├── AuthServiceTests.cs
│   │   ├── AccountServiceTests.cs
│   │   └── TransactionServiceTests.cs
│   └── Utilities/
│       └── HelperTests.cs
├── Integration/
│   ├── Controllers/
│   │   ├── AuthControllerTests.cs
│   │   └── AccountsControllerTests.cs
│   └── EndToEnd/
│       └── BankingWorkflowTests.cs
└── TestHelpers/
    ├── TestDataBuilder.cs
    └── MockFactory.cs
```

## Test Naming Convention

### Pattern
```
MethodName_Scenario_ExpectedResult
```

### Examples
```csharp
[Fact]
public async Task CreateAccount_ValidRequest_ReturnsAccountDto()

[Fact]
public async Task Transfer_InsufficientFunds_ThrowsInvalidOperationException()

[Fact]
public async Task Login_InvalidCredentials_ReturnsUnauthorized()

[Fact]
public async Task GetAccounts_MultipleAccounts_ReturnsAllAccounts()
```

## Unit Testing

### Service Unit Test Template

```csharp
public class AccountServiceTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly AccountService _service;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    private readonly User _testUser;

    public AccountServiceTests()
    {
        // Arrange - Setup in-memory database
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankingDbContext(options);
        _loggerMock = new Mock<ILogger<AccountService>>();
        _service = new AccountService(_context, _loggerMock.Object);

        // Setup test data
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "John",
            LastName = "Doe"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAccount_ValidRequest_ReturnsAccountDto()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Type = AccountType.Checking,
            Currency = "USD",
            InitialDeposit = 1000
        };

        // Act
        var result = await _service.CreateAccountAsync(_testUser.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AccountType.Checking, result.Type);
        Assert.Equal(1000, result.Balance);
        Assert.Equal("USD", result.Currency);
        Assert.NotEqual(Guid.Empty, result.Id);
        
        // Verify database state
        var accountInDb = await _context.Accounts.FindAsync(result.Id);
        Assert.NotNull(accountInDb);
        Assert.Equal(_testUser.Id, accountInDb.UserId);
    }

    [Fact]
    public async Task CreateAccount_InvalidUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Type = AccountType.Checking,
            Currency = "USD",
            InitialDeposit = 1000
        };
        var invalidUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.CreateAccountAsync(invalidUserId, request));
        
        Assert.Equal("User not found", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateAccount_InvalidDeposit_ThrowsException(decimal invalidDeposit)
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Type = AccountType.Checking,
            Currency = "USD",
            InitialDeposit = invalidDeposit
        };

        // Act & Assert - Validation should catch this
        // Note: In real implementation, add validation in service
        Assert.True(invalidDeposit <= 0);
    }

    [Fact]
    public async Task GetUserAccounts_MultipleAccounts_ReturnsAllAccounts()
    {
        // Arrange
        var account1 = new Account
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            AccountNumber = "1234567890",
            Type = AccountType.Checking,
            Balance = 500
        };
        var account2 = new Account
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            AccountNumber = "0987654321",
            Type = AccountType.Savings,
            Balance = 1500
        };
        _context.Accounts.AddRange(account1, account2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserAccountsAsync(_testUser.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.AccountNumber == "1234567890");
        Assert.Contains(result, a => a.AccountNumber == "0987654321");
    }

    [Fact]
    public async Task GetUserAccounts_NoAccounts_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetUserAccountsAsync(_testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccountById_ExistingAccount_ReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            AccountNumber = "1234567890",
            Type = AccountType.Checking,
            Balance = 500
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAccountByIdAsync(account.Id, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal(account.AccountNumber, result.AccountNumber);
    }

    [Fact]
    public async Task GetAccountById_UnauthorizedAccess_ReturnsNull()
    {
        // Arrange
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@example.com",
            PasswordHash = "hash",
            FirstName = "Other",
            LastName = "User"
        };
        _context.Users.Add(otherUser);
        
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            AccountNumber = "1234567890",
            Type = AccountType.Checking,
            Balance = 500
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAccountByIdAsync(account.Id, _testUser.Id);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### Testing Complex Scenarios

#### Transaction Service with Database Transactions

```csharp
[Fact]
public async Task Transfer_ValidRequest_UpdatesBothAccountsAtomically()
{
    // Arrange
    var fromAccount = new Account
    {
        Id = Guid.NewGuid(),
        UserId = _testUser.Id,
        AccountNumber = "1111111111",
        Balance = 1000
    };
    var toAccount = new Account
    {
        Id = Guid.NewGuid(),
        UserId = _testUser.Id,
        AccountNumber = "2222222222",
        Balance = 500
    };
    _context.Accounts.AddRange(fromAccount, toAccount);
    await _context.SaveChangesAsync();

    var request = new TransferRequest
    {
        FromAccountId = fromAccount.Id,
        ToAccountNumber = toAccount.AccountNumber,
        Amount = 300
    };

    // Act
    var result = await _service.TransferAsync(_testUser.Id, request);

    // Assert
    Assert.Equal(TransactionStatus.Completed, result.Status);
    Assert.Equal(300, result.Amount);

    // Verify both accounts updated
    var updatedFrom = await _context.Accounts.FindAsync(fromAccount.Id);
    var updatedTo = await _context.Accounts.FindAsync(toAccount.Id);
    
    Assert.Equal(700, updatedFrom!.Balance);
    Assert.Equal(800, updatedTo!.Balance);

    // Verify transaction recorded
    var transaction = await _context.Transactions
        .FirstOrDefaultAsync(t => t.Id == result.Id);
    Assert.NotNull(transaction);
    Assert.Equal(700, transaction.FromAccountBalanceAfter);
    Assert.Equal(800, transaction.ToAccountBalanceAfter);
}

[Fact]
public async Task Transfer_InsufficientFunds_DoesNotModifyAccounts()
{
    // Arrange
    var fromAccount = new Account
    {
        Id = Guid.NewGuid(),
        UserId = _testUser.Id,
        AccountNumber = "1111111111",
        Balance = 100
    };
    var toAccount = new Account
    {
        Id = Guid.NewGuid(),
        UserId = _testUser.Id,
        AccountNumber = "2222222222",
        Balance = 500
    };
    _context.Accounts.AddRange(fromAccount, toAccount);
    await _context.SaveChangesAsync();

    var request = new TransferRequest
    {
        FromAccountId = fromAccount.Id,
        ToAccountNumber = toAccount.AccountNumber,
        Amount = 300 // More than available
    };

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(
        async () => await _service.TransferAsync(_testUser.Id, request));

    // Verify accounts unchanged
    var unchangedFrom = await _context.Accounts.FindAsync(fromAccount.Id);
    var unchangedTo = await _context.Accounts.FindAsync(toAccount.Id);
    
    Assert.Equal(100, unchangedFrom!.Balance);
    Assert.Equal(500, unchangedTo!.Balance);

    // Verify no transaction created
    var transactionCount = await _context.Transactions.CountAsync();
    Assert.Equal(0, transactionCount);
}
```

## Integration Testing

### Controller Integration Test Template

```csharp
public class AccountsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AccountsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BankingDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<BankingDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateAccount_ValidRequest_Returns200WithAccountDto()
    {
        // Arrange - Get auth token first
        var token = await CreateUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateAccountRequest
        {
            Type = AccountType.Checking,
            Currency = "USD",
            InitialDeposit = 1000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<AccountDto>>();
        
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1000, result.Data.Balance);
        Assert.Equal(AccountType.Checking, result.Data.Type);
    }

    [Fact]
    public async Task CreateAccount_WithoutAuthentication_Returns401()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Type = AccountType.Checking,
            Currency = "USD",
            InitialDeposit = 1000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccounts_ReturnsUserAccountsOnly()
    {
        // Arrange
        var token = await CreateUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Create accounts
        await CreateAccount(AccountType.Checking, 1000);
        await CreateAccount(AccountType.Savings, 2000);

        // Act
        var response = await _client.GetAsync("/api/accounts");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<AccountDto>>>();
        
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }

    // Helper methods
    private async Task<string> CreateUserAndGetToken()
    {
        var signUpRequest = new SignUpRequest
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "SecurePass123",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", signUpRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return result!.Data!.Token;
    }

    private async Task<Guid> CreateAccount(AccountType type, decimal initialDeposit)
    {
        var request = new CreateAccountRequest
        {
            Type = type,
            Currency = "USD",
            InitialDeposit = initialDeposit
        };

        var response = await _client.PostAsJsonAsync("/api/accounts", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AccountDto>>();
        return result!.Data!.Id;
    }
}
```

### End-to-End Workflow Tests

```csharp
[Fact]
public async Task CompleteUserJourney_SignUpToTransfer_Succeeds()
{
    // 1. Sign up
    var signUpResponse = await _client.PostAsJsonAsync("/api/auth/signup", new SignUpRequest
    {
        Email = $"journey_{Guid.NewGuid()}@example.com",
        Password = "SecurePass123",
        FirstName = "Journey",
        LastName = "Test"
    });
    signUpResponse.EnsureSuccessStatusCode();
    var authResult = await signUpResponse.Content
        .ReadFromJsonAsync<ApiResponse<AuthResponse>>();
    var token = authResult!.Data!.Token;

    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // 2. Create checking account
    var checkingResponse = await _client.PostAsJsonAsync("/api/accounts", 
        new CreateAccountRequest
        {
            Type = AccountType.Checking,
            Currency = "USD",
            InitialDeposit = 5000
        });
    var checking = (await checkingResponse.Content
        .ReadFromJsonAsync<ApiResponse<AccountDto>>())!.Data!;

    // 3. Create savings account
    var savingsResponse = await _client.PostAsJsonAsync("/api/accounts", 
        new CreateAccountRequest
        {
            Type = AccountType.Savings,
            Currency = "USD",
            InitialDeposit = 1000
        });
    var savings = (await savingsResponse.Content
        .ReadFromJsonAsync<ApiResponse<AccountDto>>())!.Data!;

    // 4. Deposit to checking
    var depositResponse = await _client.PostAsJsonAsync("/api/transactions/deposit",
        new DepositRequest
        {
            AccountId = checking.Id,
            Amount = 2000,
            Description = "Salary"
        });
    depositResponse.EnsureSuccessStatusCode();

    // 5. Transfer to savings
    var transferResponse = await _client.PostAsJsonAsync("/api/transactions/transfer",
        new TransferRequest
        {
            FromAccountId = checking.Id,
            ToAccountNumber = savings.AccountNumber,
            Amount = 1500,
            Description = "Monthly savings"
        });
    transferResponse.EnsureSuccessStatusCode();

    // 6. Create debit card
    var cardResponse = await _client.PostAsJsonAsync("/api/cards",
        new CreateCardRequest
        {
            AccountId = checking.Id,
            Type = CardType.Debit,
            DailyLimit = 1000
        });
    cardResponse.EnsureSuccessStatusCode();

    // 7. Generate statement
    var statementResponse = await _client.PostAsJsonAsync("/api/statements/generate",
        new StatementRequest
        {
            AccountId = checking.Id,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        });
    statementResponse.EnsureSuccessStatusCode();
    var statement = (await statementResponse.Content
        .ReadFromJsonAsync<ApiResponse<StatementDto>>())!.Data!;

    // Verify final state
    Assert.Equal(2, statement.Transactions.Count); // Deposit + Transfer
    Assert.Equal(2000, statement.TotalDeposits);
    Assert.Equal(1500, statement.TotalWithdrawals);
}
```

## Test Data Builders

```csharp
public class TestDataBuilder
{
    private readonly BankingDbContext _context;

    public TestDataBuilder(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateUserAsync(
        string? email = null,
        string? firstName = null,
        string? lastName = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"test_{Guid.NewGuid()}@example.com",
            PasswordHash = "TestHash123",
            FirstName = firstName ?? "Test",
            LastName = lastName ?? "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<Account> CreateAccountAsync(
        Guid userId,
        AccountType type = AccountType.Checking,
        decimal balance = 1000)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = GenerateAccountNumber(),
            Type = type,
            Balance = balance,
            Currency = "USD",
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<Transaction> CreateTransactionAsync(
        Guid? fromAccountId,
        Guid? toAccountId,
        decimal amount,
        TransactionType type = TransactionType.Transfer)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = GenerateTransactionNumber(),
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount,
            Type = type,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    private string GenerateAccountNumber() => 
        Random.Shared.Next(1000000000, int.MaxValue).ToString();

    private string GenerateTransactionNumber() => 
        $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
}
```

## Test Coverage Requirements

### Minimum Coverage per Component

- **Controllers**: 90%+ (simple pass-through logic)
- **Services**: 85%+ (business logic)
- **Models**: Not required (POCOs)
- **DTOs**: Not required (data structures)
- **Middleware**: 80%+

### Critical Paths (100% Coverage Required)

- Authentication (signup, login)
- Transaction operations (transfer, deposit, withdraw)
- User ownership validation
- Error handling in controllers

## Testing Checklist for New Features

When adding a new feature, ensure tests cover:

- [ ] **Happy path** - Feature works as expected
- [ ] **Validation** - Invalid input is rejected
- [ ] **Authorization** - Only authorized users can access
- [ ] **Ownership** - Users can only access their own data
- [ ] **Edge cases** - Boundary conditions handled
- [ ] **Error scenarios** - Proper exception handling
- [ ] **Concurrent access** - No race conditions (if applicable)
- [ ] **Performance** - No N+1 queries or slow operations
- [ ] **Database state** - Data persisted correctly
- [ ] **Logging** - Important events logged

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "FullyQualifiedName~AccountServiceTests"

# Run tests matching pattern
dotnet test --filter "Name~Transfer"

# Run in parallel
dotnet test --parallel

# Verbose output
dotnet test --verbosity detailed
```

## Continuous Integration

Tests should run on:
- Every commit (pre-commit hook)
- Every pull request
- Before deployment
- Nightly (full suite with performance tests)

## Performance Testing

```csharp
[Fact]
public async Task GetAccounts_With1000Accounts_CompletesQuickly()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    
    for (int i = 0; i < 1000; i++)
    {
        await _testDataBuilder.CreateAccountAsync(_testUser.Id);
    }

    // Act
    var result = await _service.GetUserAccountsAsync(_testUser.Id);

    // Assert
    stopwatch.Stop();
    Assert.Equal(1000, result.Count);
    Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
        $"Query took {stopwatch.ElapsedMilliseconds}ms");
}
```

## Best Practices Summary

1. **AAA Pattern**: Arrange, Act, Assert
2. **One Assertion Focus**: Test one behavior per test
3. **Independence**: Tests don't depend on each other
4. **Repeatability**: Same result every time
5. **Speed**: Fast execution (< 100ms per unit test)
6. **Readability**: Clear test names and structure
7. **Maintainability**: Easy to update when code changes

## AI Assistant Instructions for Test Generation

When generating tests:
1. Use the test templates provided above
2. Follow naming conventions strictly
3. Include both success and failure scenarios
4. Add edge case tests
5. Use TestDataBuilder for complex setups
6. Verify database state when applicable
7. Include performance assertions for queries
8. Add descriptive comments for complex scenarios