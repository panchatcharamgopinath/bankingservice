using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
