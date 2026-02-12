using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingService.Models;
public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}
