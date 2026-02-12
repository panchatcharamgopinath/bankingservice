using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


public class DepositRequest
{
    [Required]
    public Guid AccountId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
}

