using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingService.Models;

public enum AccountType
{
    Checking,
    Savings,
    Investment
}
