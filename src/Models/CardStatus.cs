using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingService.Models;
public enum CardStatus
{
    Active,
    Blocked,
    Expired,
    Cancelled
}