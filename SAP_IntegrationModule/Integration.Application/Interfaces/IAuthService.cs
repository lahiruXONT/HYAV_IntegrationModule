using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.DTOs;
using Integration.Application.Services;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> AuthenticateAsync(AuthRequestDto request, string? ipAddress = null);
    Task<AuthResponseDto> RefreshTokenAsync(
        string refreshToken,
        string userName,
        string? ipAddress = null
    );
    Task LogoutAsync(string refreshToken, string userName);
    Task<UserDto> CreateUserAsync(CreateUserDto userDto, string createdBy);
    List<string> ValidateAuthRequest(AuthRequestDto request);
}
