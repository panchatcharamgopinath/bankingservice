using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BankingService.DTOs;
using BankingService.Services;

namespace BankingService.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StatementsController : ControllerBase
{
    private readonly IStatementService _statementService;
    private readonly ILogger<StatementsController> _logger;

    public StatementsController(IStatementService statementService, ILogger<StatementsController> logger)
    {
        _statementService = statementService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<StatementDto>>> GenerateStatement([FromBody] StatementRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _statementService.GenerateStatementAsync(userId, request);
            return Ok(new ApiResponse<StatementDto>
            {
                Success = true,
                Data = result,
                Message = "Statement generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating statement");
            return BadRequest(new ApiResponse<StatementDto>
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