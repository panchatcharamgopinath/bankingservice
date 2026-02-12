using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BankingService.DTOs;
using BankingService.Services;

namespace BankingService.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Transfer([FromBody] TransferRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transactionService.TransferAsync(userId, request);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Transfer completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transfer");
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Deposit([FromBody] DepositRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transactionService.DepositAsync(userId, request);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Deposit completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deposit");
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Withdraw([FromBody] WithdrawalRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transactionService.WithdrawAsync(userId, request);
            return Ok(new ApiResponse<TransactionDto>
            {
                Success = true,
                Data = result,
                Message = "Withdrawal completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during withdrawal");
            return BadRequest(new ApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetAccountTransactions(Guid accountId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _transactionService.GetAccountTransactionsAsync(accountId, userId);
            return Ok(new ApiResponse<List<TransactionDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return BadRequest(new ApiResponse<List<TransactionDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
