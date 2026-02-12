using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using BankingService.Data;
using BankingService.DTOs;
using BankingService.Models;
using BankingService.Services;

namespace BankingService.Tests;

public class AccountServiceTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly AccountService _service;
    private readonly User _testUser;

    public AccountServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankingDbContext(options);
        var logger = new Mock<ILogger<AccountService>>().Object;
        _service = new AccountService(_context, logger);

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
    }

    [Fact]
    public async Task GetUserAccounts_ReturnsAllUserAccounts()
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
    }

    [Fact]
    public async Task GetAccountById_NonExistentAccount_ReturnsNull()
    {
        // Act
        var result = await _service.GetAccountByIdAsync(Guid.NewGuid(), _testUser.Id);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
