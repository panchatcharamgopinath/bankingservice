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

public class TransactionServiceTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly TransactionService _service;
    private readonly User _testUser;
    private readonly Account _testAccount1;
    private readonly Account _testAccount2;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankingDbContext(options);
        var logger = new Mock<ILogger<TransactionService>>().Object;
        _service = new TransactionService(_context, logger);

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "John",
            LastName = "Doe"
        };

        _testAccount1 = new Account
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            AccountNumber = "1234567890",
            Type = AccountType.Checking,
            Balance = 1000
        };

        _testAccount2 = new Account
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            AccountNumber = "0987654321",
            Type = AccountType.Savings,
            Balance = 500
        };

        _context.Users.Add(_testUser);
        _context.Accounts.AddRange(_testAccount1, _testAccount2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Transfer_ValidRequest_CompletesSuccessfully()
    {
        // Arrange
        var request = new TransferRequest
        {
            FromAccountId = _testAccount1.Id,
            ToAccountNumber = _testAccount2.AccountNumber,
            Amount = 200,
            Description = "Test transfer"
        };

        // Act
        var result = await _service.TransferAsync(_testUser.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransactionStatus.Completed, result.Status);
        Assert.Equal(200, result.Amount);

        var fromAccount = await _context.Accounts.FindAsync(_testAccount1.Id);
        var toAccount = await _context.Accounts.FindAsync(_testAccount2.Id);
        Assert.Equal(800, fromAccount!.Balance);
        Assert.Equal(700, toAccount!.Balance);
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var request = new TransferRequest
        {
            FromAccountId = _testAccount1.Id,
            ToAccountNumber = _testAccount2.AccountNumber,
            Amount = 2000,
            Description = "Test transfer"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.TransferAsync(_testUser.Id, request));
    }

    [Fact]
    public async Task Deposit_ValidRequest_IncreasesBalance()
    {
        // Arrange
        var request = new DepositRequest
        {
            AccountId = _testAccount1.Id,
            Amount = 500,
            Description = "Test deposit"
        };

        // Act
        var result = await _service.DepositAsync(_testUser.Id, request);

        // Assert
        Assert.Equal(TransactionStatus.Completed, result.Status);
        var account = await _context.Accounts.FindAsync(_testAccount1.Id);
        Assert.Equal(1500, account!.Balance);
    }

    [Fact]
    public async Task Withdraw_ValidRequest_DecreasesBalance()
    {
        // Arrange
        var request = new WithdrawalRequest
        {
            AccountId = _testAccount1.Id,
            Amount = 300,
            Description = "Test withdrawal"
        };

        // Act
        var result = await _service.WithdrawAsync(_testUser.Id, request);

        // Assert
        Assert.Equal(TransactionStatus.Completed, result.Status);
        var account = await _context.Accounts.FindAsync(_testAccount1.Id);
        Assert.Equal(700, account!.Balance);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var request = new WithdrawalRequest
        {
            AccountId = _testAccount1.Id,
            Amount = 2000,
            Description = "Test withdrawal"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.WithdrawAsync(_testUser.Id, request));
    }

    [Fact]
    public async Task GetAccountTransactions_ReturnsAllTransactions()
    {
        // Arrange
        var transaction1 = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = "TXN001",
            FromAccountId = _testAccount1.Id,
            Amount = 100,
            Type = TransactionType.Withdrawal,
            Status = TransactionStatus.Completed
        };
        var transaction2 = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = "TXN002",
            ToAccountId = _testAccount1.Id,
            Amount = 200,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed
        };
        _context.Transactions.AddRange(transaction1, transaction2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAccountTransactionsAsync(_testAccount1.Id, _testUser.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
