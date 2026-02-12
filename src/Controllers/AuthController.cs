using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BankingService.DTOs;
using BankingService.Services;

namespace BankingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> SignUp([FromBody] SignUpRequest request)
    {
        try
        {
            var result = await _authService.SignUpAsync(request);
            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Data = result,
                Message = "User registered successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign up");
            return BadRequest(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Data = result,
                Message = "Login successful"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized login attempt");
            return Unauthorized(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return BadRequest(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}
