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


public interface IStatementService
{
    Task<StatementDto> GenerateStatementAsync(Guid userId, StatementRequest request);
}

public class StatementService : IStatementService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<StatementService> _logger;

    public StatementService(BankingDbContext context, ILogger<StatementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StatementDto> GenerateStatementAsync(Guid userId, StatementRequest request)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);

        if (account == null)
            throw new InvalidOperationException("Account not found");

        var transactions = await _context.Transactions
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t => (t.FromAccountId == request.AccountId || t.ToAccountId == request.AccountId)
                && t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate
                && t.Status == TransactionStatus.Completed)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var deposits = transactions.Where(t => t.ToAccountId == request.AccountId).Sum(t => t.Amount);
        var withdrawals = transactions.Where(t => t.FromAccountId == request.AccountId).Sum(t => t.Amount);

        var statement = new StatementDto
        {
            Account = new AccountDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Type = account.Type,
                Balance = account.Balance,
                Currency = account.Currency,
                Status = account.Status,
                CreatedAt = account.CreatedAt
            },
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ClosingBalance = account.Balance,
            OpeningBalance = account.Balance - deposits + withdrawals,
            TotalDeposits = deposits,
            TotalWithdrawals = withdrawals,
            Transactions = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                FromAccountNumber = t.FromAccount?.AccountNumber,
                ToAccountNumber = t.ToAccount?.AccountNumber,
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            }).ToList()
        };

        _logger.LogInformation("Statement generated for account: {AccountId}", request.AccountId);

        return statement;
    }
}