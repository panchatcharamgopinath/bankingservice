using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingService.Models;

public class Card
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string CardNumber { get; set; } = string.Empty;
    
    [Required]
    public Guid AccountId { get; set; }
    
    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; } = null!;
    
    [Required]
    public CardType Type { get; set; }
    
    [Required]
    public string CardHolderName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(3)]
    public string Cvv { get; set; } = string.Empty;
    
    public DateTime ExpiryDate { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal DailyLimit { get; set; }
    
    public CardStatus Status { get; set; } = CardStatus.Active;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? BlockedAt { get; set; }
}

