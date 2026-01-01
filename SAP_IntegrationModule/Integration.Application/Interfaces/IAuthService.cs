using Integration.Application.DTOs;
using Integration.Application.Services;
using Integration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Interfaces
{

    public interface IAuthService
    {
        Task<AuthResponseDto> AuthenticateAsync(AuthRequestDto request, string? ipAddress = null);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
        Task LogoutAsync(string refreshToken);
        Task<UserDto> CreateUserAsync(CreateUserDto userDto, string createdBy);
        List<string> ValidateAuthRequest(AuthRequestDto request);
    }
}
