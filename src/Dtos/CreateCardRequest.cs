using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


// Card DTOs
public class CreateCardRequest
{
    [Required]
    public Guid AccountId { get; set; }
    
    [Required]
    public CardType Type { get; set; }
    
    [Range(100, 50000)]
    public decimal DailyLimit { get; set; } = 1000;
}
