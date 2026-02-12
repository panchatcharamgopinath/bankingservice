using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


// Statement DTOs
public class StatementRequest
{
    [Required]
    public Guid AccountId { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
}
