using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


public class TransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string? FromAccountNumber { get; set; }
    public string? ToAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
