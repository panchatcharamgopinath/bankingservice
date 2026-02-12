using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


public class StatementDto
{
    public AccountDto Account { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new();
}
