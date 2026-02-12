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


public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountRequest request);
    Task<List<AccountDto>> GetUserAccountsAsync(Guid userId);
    Task<AccountDto?> GetAccountByIdAsync(Guid accountId, Guid userId);
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

    public async Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountRequest request)
    {
        _logger.LogInformation("Creating account for user: {UserId}, Type: {Type}", userId, request.Type);

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = GenerateAccountNumber(),
            Type = request.Type,
            Currency = request.Currency,
            Balance = request.InitialDeposit,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Account created: {AccountId}, Number: {AccountNumber}", account.Id, account.AccountNumber);

        return MapToAccountDto(account);
    }

    public async Task<List<AccountDto>> GetUserAccountsAsync(Guid userId)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return accounts.Select(MapToAccountDto).ToList();
    }

    public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        return account != null ? MapToAccountDto(account) : null;
    }

    private string GenerateAccountNumber()
    {
        return DateTime.UtcNow.Ticks.ToString()[^10..];
    }

    private AccountDto MapToAccountDto(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            Type = account.Type,
            Balance = account.Balance,
            Currency = account.Currency,
            Status = account.Status,
            CreatedAt = account.CreatedAt
        };
    }
}
