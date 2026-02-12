using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BankingService.DTOs;
using BankingService.Services;

namespace BankingService.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountDto>>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _accountService.CreateAccountAsync(userId, request);
            return Ok(new ApiResponse<AccountDto>
            {
                Success = true,
                Data = result,
                Message = "Account created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return BadRequest(new ApiResponse<AccountDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AccountDto>>>> GetAccounts()
    {
        try
        {
            var userId = GetUserId();
            var result = await _accountService.GetUserAccountsAsync(userId);
            return Ok(new ApiResponse<List<AccountDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return BadRequest(new ApiResponse<List<AccountDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("{accountId}")]
    public async Task<ActionResult<ApiResponse<AccountDto>>> GetAccount(Guid accountId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _accountService.GetAccountByIdAsync(accountId, userId);
            if (result == null)
                return NotFound(new ApiResponse<AccountDto>
                {
                    Success = false,
                    Message = "Account not found"
                });

            return Ok(new ApiResponse<AccountDto>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account");
            return BadRequest(new ApiResponse<AccountDto>
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
