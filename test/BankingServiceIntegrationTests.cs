using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using BankingService.Data;
using BankingService.DTOs;
using BankingService.Models;

namespace BankingService.Tests.Integration;

public class BankingServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BankingServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BankingDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<BankingDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SignUp_Login_CreateAccount_Flow()
    {
        // 1. Sign up
        var signUpRequest = new SignUpRequest
        {
            Email = $"integrationtest_{Guid.NewGuid()}@example.com",
            Password = "SecurePass123",
            FirstName = "Integration",
            LastName = "Test"
        };

        var signUpResponse = await _client.PostAsJsonAsync("/api/auth/signup", signUpRequest);
        signUpResponse.EnsureSuccessStatusCode();

        var signUpResult = await signUpResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(signUpResult);
        Assert.True(signUpResult.Success);
        Assert.NotNull(signUpResult.Data);
        Assert.NotEmpty(signUpResult.Data.Token);

        var token = signUpResult.Data.Token;

        // 2. Create account
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createAccountRequest = new CreateAccountRequest
        {
            Type = AccountType.Checking,
            Currency = "USD",
            InitialDeposit = 1000
        };

        var createAccountResponse = await _client.PostAsJsonAsync("/api/accounts", createAccountRequest);
        createAccountResponse.EnsureSuccessStatusCode();

        var accountResult = await createAccountResponse.Content.ReadFromJsonAsync<ApiResponse<AccountDto>>();
        Assert.NotNull(accountResult);
        Assert.True(accountResult.Success);
        Assert.NotNull(accountResult.Data);
        Assert.Equal(1000, accountResult.Data.Balance);
    }

    [Fact]
    public async Task CompleteTransactionFlow()
    {
        // Setup: Create user and accounts
        var token = await CreateUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var account1Id = await CreateAccount(AccountType.Checking, 2000);
        var account2 = await CreateAccountAndGetDetails(AccountType.Savings, 500);

        // 1. Deposit
        var depositRequest = new DepositRequest
        {
            AccountId = account1Id,
            Amount = 500,
            Description = "Test deposit"
        };

        var depositResponse = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);
        depositResponse.EnsureSuccessStatusCode();

        var depositResult = await depositResponse.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        Assert.NotNull(depositResult);
        Assert.True(depositResult.Success);
        Assert.Equal(500, depositResult.Data!.Amount);
        Assert.Equal(TransactionStatus.Completed, depositResult.Data.Status);

        // 2. Transfer
        var transferRequest = new TransferRequest
        {
            FromAccountId = account1Id,
            ToAccountNumber = account2.AccountNumber,
            Amount = 300,
            Description = "Test transfer"
        };

        var transferResponse = await _client.PostAsJsonAsync("/api/transactions/transfer", transferRequest);
        transferResponse.EnsureSuccessStatusCode();

        var transferResult = await transferResponse.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        Assert.NotNull(transferResult);
        Assert.True(transferResult.Success);
        Assert.Equal(300, transferResult.Data!.Amount);

        // 3. Withdraw
        var withdrawRequest = new WithdrawalRequest
        {
            AccountId = account1Id,
            Amount = 200,
            Description = "Test withdrawal"
        };

        var withdrawResponse = await _client.PostAsJsonAsync("/api/transactions/withdraw", withdrawRequest);
        withdrawResponse.EnsureSuccessStatusCode();

        // 4. Get transactions
        var transactionsResponse = await _client.GetAsync($"/api/transactions/account/{account1Id}");
        transactionsResponse.EnsureSuccessStatusCode();

        var transactionsResult = await transactionsResponse.Content.ReadFromJsonAsync<ApiResponse<List<TransactionDto>>>();
        Assert.NotNull(transactionsResult);
        Assert.True(transactionsResult.Success);
        Assert.NotEmpty(transactionsResult.Data!);
    }

    [Fact]
    public async Task CardManagement_Flow()
    {
        // Setup
        var token = await CreateUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var accountId = await CreateAccount(AccountType.Checking, 5000);

        // 1. Create card
        var createCardRequest = new CreateCardRequest
        {
            AccountId = accountId,
            Type = CardType.Debit,
            DailyLimit = 1000
        };

        var createCardResponse = await _client.PostAsJsonAsync("/api/cards", createCardRequest);
        createCardResponse.EnsureSuccessStatusCode();

        var cardResult = await createCardResponse.Content.ReadFromJsonAsync<ApiResponse<CardDto>>();
        Assert.NotNull(cardResult);
        Assert.True(cardResult.Success);
        Assert.NotNull(cardResult.Data);
        Assert.Equal(CardStatus.Active, cardResult.Data.Status);

        var cardId = cardResult.Data.Id;

        // 2. Get cards
        var getCardsResponse = await _client.GetAsync($"/api/cards/account/{accountId}");
        getCardsResponse.EnsureSuccessStatusCode();

        var cardsResult = await getCardsResponse.Content.ReadFromJsonAsync<ApiResponse<List<CardDto>>>();
        Assert.NotNull(cardsResult);
        Assert.True(cardsResult.Success);
        Assert.Single(cardsResult.Data!);

        // 3. Block card
        var blockCardResponse = await _client.PutAsync($"/api/cards/{cardId}/block", null);
        blockCardResponse.EnsureSuccessStatusCode();

        var blockedCardResult = await blockCardResponse.Content.ReadFromJsonAsync<ApiResponse<CardDto>>();
        Assert.NotNull(blockedCardResult);
        Assert.True(blockedCardResult.Success);
        Assert.Equal(CardStatus.Blocked, blockedCardResult.Data!.Status);
    }

    [Fact]
    public async Task Statement_Generation()
    {
        // Setup
        var token = await CreateUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var accountId = await CreateAccount(AccountType.Checking, 1000);

        // Create some transactions
        await _client.PostAsJsonAsync("/api/transactions/deposit", new DepositRequest
        {
            AccountId = accountId,
            Amount = 500
        });

        await _client.PostAsJsonAsync("/api/transactions/withdraw", new WithdrawalRequest
        {
            AccountId = accountId,
            Amount = 200
        });

        // Generate statement
        var statementRequest = new StatementRequest
        {
            AccountId = accountId,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var statementResponse = await _client.PostAsJsonAsync("/api/statements/generate", statementRequest);
        statementResponse.EnsureSuccessStatusCode();

        var statementResult = await statementResponse.Content.ReadFromJsonAsync<ApiResponse<StatementDto>>();
        Assert.NotNull(statementResult);
        Assert.True(statementResult.Success);
        Assert.NotNull(statementResult.Data);
        Assert.NotEmpty(statementResult.Data.Transactions);
    }

    [Fact]
    public async Task Unauthorized_Request_Returns401()
    {
        // Act - attempt to access protected endpoint without token
        var response = await _client.GetAsync("/api/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private async Task<AccountDto> CreateAccountAndGetDetails(AccountType type, decimal initialDeposit)
    {
        var request = new CreateAccountRequest
        {
            Type = type,
            Currency = "USD",
            InitialDeposit = initialDeposit
        };

        var response = await _client.PostAsJsonAsync("/api/accounts", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AccountDto>>();
        return result!.Data!;
    }
}