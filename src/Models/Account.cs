using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingService.Models;

public class Account
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string AccountNumber { get; set; } = string.Empty;
    
    [Required]
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    [Required]
    public AccountType Type { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Balance { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    
    public virtual ICollection<Transaction> TransactionsFrom { get; set; } = new List<Transaction>();
    public virtual ICollection<Transaction> TransactionsTo { get; set; } = new List<Transaction>();
    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}
