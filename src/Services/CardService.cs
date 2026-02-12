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



public interface ICardService
{
    Task<CardDto> CreateCardAsync(Guid userId, CreateCardRequest request);
    Task<List<CardDto>> GetAccountCardsAsync(Guid accountId, Guid userId);
    Task<CardDto?> BlockCardAsync(Guid cardId, Guid userId);
}

public class CardService : ICardService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<CardService> _logger;

    public CardService(BankingDbContext context, ILogger<CardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CardDto> CreateCardAsync(Guid userId, CreateCardRequest request)
    {
        var account = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);

        if (account == null)
            throw new InvalidOperationException("Account not found");

        var card = new Card
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            CardNumber = GenerateCardNumber(),
            Type = request.Type,
            CardHolderName = $"{account.User.FirstName} {account.User.LastName}",
            Cvv = GenerateCvv(),
            ExpiryDate = DateTime.UtcNow.AddYears(3),
            DailyLimit = request.DailyLimit,
            Status = CardStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow
        };

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Card created: {CardId}", card.Id);

        return MapToCardDto(card);
    }

    public async Task<List<CardDto>> GetAccountCardsAsync(Guid accountId, Guid userId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
            throw new InvalidOperationException("Account not found");

        var cards = await _context.Cards
            .Where(c => c.AccountId == accountId)
            .ToListAsync();

        return cards.Select(MapToCardDto).ToList();
    }

    public async Task<CardDto?> BlockCardAsync(Guid cardId, Guid userId)
    {
        var card = await _context.Cards
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Account.UserId == userId);

        if (card == null)
            return null;

        card.Status = CardStatus.Blocked;
        card.BlockedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Card blocked: {CardId}", cardId);

        return MapToCardDto(card);
    }

    private string GenerateCardNumber()
    {
        return $"{Random.Shared.Next(1000, 9999)}{Random.Shared.Next(1000, 9999)}{Random.Shared.Next(1000, 9999)}{Random.Shared.Next(1000, 9999)}";
    }

    private string GenerateCvv()
    {
        return Random.Shared.Next(100, 999).ToString();
    }

    private CardDto MapToCardDto(Card card)
    {
        return new CardDto
        {
            Id = card.Id,
            CardNumber = MaskCardNumber(card.CardNumber),
            CardHolderName = card.CardHolderName,
            Type = card.Type,
            ExpiryDate = card.ExpiryDate,
            DailyLimit = card.DailyLimit,
            Status = card.Status,
            CreatedAt = card.CreatedAt
        };
    }

    private string MaskCardNumber(string cardNumber)
    {
        if (cardNumber.Length < 4)
            return cardNumber;
        return $"****-****-****-{cardNumber[^4..]}";
    }
}
