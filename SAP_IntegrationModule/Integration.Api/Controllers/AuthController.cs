using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Integration.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService,ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] AuthRequestDto request)
        {
            try
            {
                var validationErrors = _authService.ValidateAuthRequest(request);
                if (validationErrors.Any())
                {
                    _logger.LogWarning("Login failed for user {Username}: {Message}", request.Username, validationErrors);
                    return BadRequest(new { errors = validationErrors });
                }

                var ipAddress = GetClientIpAddress();
                var result = await _authService.AuthenticateAsync(request, ipAddress);

                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for user {Username}: {Message}", request.Username, result.Message);
                    return Unauthorized(result.Message);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user {Username}", request.Username);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again."
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

                if (!result.Success)
                {
                    _logger.LogWarning("Token refresh failed {Username}: {Message}", result?.User?.Username ?? "", result?.Message);
                    return Unauthorized(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, new { error = "Token refresh failed" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            try
            {
                await _authService.LogoutAsync(request.RefreshToken);
                return Ok(new { success = true, message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return StatusCode(500, new { error = "Logout failed" });
            }
        }        

        [HttpPost("users")]
        [Authorize]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto userDto)
        {
            try
            {
                var createdBy = User.Identity?.Name ?? "System";
                var user = await _authService.CreateUserAsync(userDto, createdBy);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user {Username}", userDto.Username);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string GetClientIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

   
}