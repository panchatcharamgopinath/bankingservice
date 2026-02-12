using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BankingService.DTOs;
using BankingService.Services;

namespace BankingService.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;
    private readonly ILogger<CardsController> _logger;

    public CardsController(ICardService cardService, ILogger<CardsController> logger)
    {
        _cardService = cardService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CardDto>>> CreateCard([FromBody] CreateCardRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _cardService.CreateCardAsync(userId, request);
            return Ok(new ApiResponse<CardDto>
            {
                Success = true,
                Data = result,
                Message = "Card created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card");
            return BadRequest(new ApiResponse<CardDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ApiResponse<List<CardDto>>>> GetAccountCards(Guid accountId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _cardService.GetAccountCardsAsync(accountId, userId);
            return Ok(new ApiResponse<List<CardDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cards");
            return BadRequest(new ApiResponse<List<CardDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("{cardId}/block")]
    public async Task<ActionResult<ApiResponse<CardDto>>> BlockCard(Guid cardId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _cardService.BlockCardAsync(cardId, userId);
            if (result == null)
                return NotFound(new ApiResponse<CardDto>
                {
                    Success = false,
                    Message = "Card not found"
                });

            return Ok(new ApiResponse<CardDto>
            {
                Success = true,
                Data = result,
                Message = "Card blocked successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking card");
            return BadRequest(new ApiResponse<CardDto>
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
