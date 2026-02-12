using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BankingService.Data;
using BankingService.Models;
using BankingService.DTOs;

namespace BankingService.Services;


public interface ITransactionService
{
    Task<TransactionDto> TransferAsync(Guid userId, TransferRequest request);
    Task<TransactionDto> DepositAsync(Guid userId, DepositRequest request);
    Task<TransactionDto> WithdrawAsync(Guid userId, WithdrawalRequest request);
    Task<List<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, Guid userId);
}

public class TransactionService : ITransactionService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(BankingDbContext context, ILogger<TransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TransactionDto> TransferAsync(Guid userId, TransferRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var fromAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId);

            if (fromAccount == null)
                throw new InvalidOperationException("Source account not found");

            var toAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber);

            if (toAccount == null)
                throw new InvalidOperationException("Destination account not found");

            if (fromAccount.Balance < request.Amount)
                throw new InvalidOperationException("Insufficient funds");

            fromAccount.Balance -= request.Amount;
            toAccount.Balance += request.Amount;

            var txn = new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionNumber = GenerateTransactionNumber(),
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = request.Amount,
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Completed,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                FromAccountBalanceAfter = fromAccount.Balance,
                ToAccountBalanceAfter = toAccount.Balance
            };

            _context.Transactions.Add(txn);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Transfer completed: {TransactionId}, Amount: {Amount}", txn.Id, request.Amount);

            return MapToTransactionDto(txn, fromAccount.AccountNumber, toAccount.AccountNumber);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransactionDto> DepositAsync(Guid userId, DepositRequest request)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);

        if (account == null)
            throw new InvalidOperationException("Account not found");

        account.Balance += request.Amount;

        var txn = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = GenerateTransactionNumber(),
            ToAccountId = account.Id,
            Amount = request.Amount,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ToAccountBalanceAfter = account.Balance
        };

        _context.Transactions.Add(txn);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deposit completed: {TransactionId}, Amount: {Amount}", txn.Id, request.Amount);

        return MapToTransactionDto(txn, null, account.AccountNumber);
    }

    public async Task<TransactionDto> WithdrawAsync(Guid userId, WithdrawalRequest request)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);

        if (account == null)
            throw new InvalidOperationException("Account not found");

        if (account.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient funds");

        account.Balance -= request.Amount;

        var txn = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionNumber = GenerateTransactionNumber(),
            FromAccountId = account.Id,
            Amount = request.Amount,
            Type = TransactionType.Withdrawal,
            Status = TransactionStatus.Completed,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            FromAccountBalanceAfter = account.Balance
        };

        _context.Transactions.Add(txn);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Withdrawal completed: {TransactionId}, Amount: {Amount}", txn.Id, request.Amount);

        return MapToTransactionDto(txn, account.AccountNumber, null);
    }

    public async Task<List<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
            throw new InvalidOperationException("Account not found");

        var transactions = await _context.Transactions
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(t => MapToTransactionDto(t, 
            t.FromAccount?.AccountNumber, 
            t.ToAccount?.AccountNumber)).ToList();
    }

    private string GenerateTransactionNumber()
    {
        return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    private TransactionDto MapToTransactionDto(Transaction txn, string? fromAccNum, string? toAccNum)
    {
        return new TransactionDto
        {
            Id = txn.Id,
            TransactionNumber = txn.TransactionNumber,
            FromAccountNumber = fromAccNum,
            ToAccountNumber = toAccNum,
            Amount = txn.Amount,
            Type = txn.Type,
            Status = txn.Status,
            Description = txn.Description,
            CreatedAt = txn.CreatedAt,
            CompletedAt = txn.CompletedAt
        };
    }
}
