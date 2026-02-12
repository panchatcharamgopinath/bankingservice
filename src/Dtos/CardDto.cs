using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


public class CardDto
{
    public Guid Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public CardType Type { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal DailyLimit { get; set; }
    public CardStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
