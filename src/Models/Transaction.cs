using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingService.Models;
public class Transaction
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string TransactionNumber { get; set; } = string.Empty;
    
    public Guid? FromAccountId { get; set; }
    
    [ForeignKey(nameof(FromAccountId))]
    public virtual Account? FromAccount { get; set; }
    
    public Guid? ToAccountId { get; set; }
    
    [ForeignKey(nameof(ToAccountId))]
    public virtual Account? ToAccount { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    
    [Required]
    public TransactionType Type { get; set; }
    
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? FromAccountBalanceAfter { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ToAccountBalanceAfter { get; set; }
}
