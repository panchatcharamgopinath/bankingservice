using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;



// Account DTOs
public class CreateAccountRequest
{
    [Required]
    public AccountType Type { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Range(0, double.MaxValue)]
    public decimal InitialDeposit { get; set; }
}
