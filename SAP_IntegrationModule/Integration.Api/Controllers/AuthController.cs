using System.Security.Claims;
using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Integration.Api.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/[controller]")]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [NonAction]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] AuthRequestDto request
    )
    {
        var validationErrors = _authService.ValidateAuthRequest(request);
        if (validationErrors.Any())
        {
            _logger.LogWarning(
                "Login failed for user {Username}: {Message}",
                request.Username,
                validationErrors
            );
            return BadRequest(
                new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Validation failed",
                    Data = null,
                    ErrorCode = ErrorCodes.Validation,
                }
            );
        }

        var ipAddress = GetClientIpAddress();
        var result = await _authService.AuthenticateAsync(request, ipAddress);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Login failed for user {Username}: {Message}",
                request.Username,
                result.Message
            );
            return Unauthorized(
                new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = result.Message,
                    Data = null,
                    ErrorCode = "Unauthorized",
                }
            );
        }

        return Ok(
            new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Login successful",
                Data = result,
            }
        );
    }

    [NonAction]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenRequestDto request
    )
    {
        var ipAddress = GetClientIpAddress();
        var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _authService.RefreshTokenAsync(
            request.RefreshToken,
            userName,
            ipAddress
        );

        if (!result.Success)
        {
            _logger.LogWarning(
                "Token refresh failed for {Username}: {Message}",
                result?.User?.Username ?? "",
                result?.Message
            );
            return Unauthorized(
                new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = result?.Message,
                    Data = null,
                    ErrorCode = "Unauthorized",
                }
            );
        }

        return Ok(
            new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = result,
            }
        );
    }

    [NonAction]
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequestDto request)
    {
        var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _authService.LogoutAsync(request.RefreshToken, userName);

        return Ok(
            new ApiResponse<object>
            {
                Success = true,
                Message = "Logged out successfully",
                Data = null,
            }
        );
    }

    [NonAction]
    [HttpPost("users")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserDto userDto
    )
    {
        var userName = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
        var user = await _authService.CreateUserAsync(userDto, userName);

        return Ok(
            new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User created successfully",
                Data = user,
            }
        );
    }

    private string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            return forwardedFor.ToString().Split(',').First().Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
