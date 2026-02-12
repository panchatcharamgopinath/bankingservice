using System.ComponentModel.DataAnnotations;
using BankingService.Models;

namespace BankingService.DTOs;


// Common response
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}