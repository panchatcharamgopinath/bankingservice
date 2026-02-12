using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


// Transaction DTOs
public class TransferRequest
{
    [Required]
    public Guid FromAccountId { get; set; }
    
    [Required]
    public string ToAccountNumber { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
}
