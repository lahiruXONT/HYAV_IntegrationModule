using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Services
{


    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHashHelper _passwordHasher;

        public AuthService( IAuthRepository authRepository, IConfiguration configuration,ILogger<AuthService> logger,PasswordHashHelper passwordHasher)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> AuthenticateAsync(AuthRequestDto request, string? ipAddress = null)
        {
            try
            {
                var user = await _authRepository.GetUserAsync(request.BusinessUnit, request.Username);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid credentials"
                    };
                }

                if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid credentials"
                    };
                }

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Update user last access
                user.LastAccessedOn = DateTime.Now;
                user.UpdatedOn = DateTime.Now;
                user.UpdatedBy = user.UserName;
                await _authRepository.UpdateUserAsync(user);

                // Create user session
                var session = new UserSession
                {
                    UserID = user.RecID,
                    RefreshToken = refreshToken,
                    IssuedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7)),
                    DeviceInfo = "API Client",
                    IPAddress = ipAddress ?? "Unknown",
                    Status = "1",
                    CreatedOn = DateTime.Now,
                    UpdatedOn = DateTime.Now,
                };

                await _authRepository.CreateUserSessionAsync(session);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Authentication successful",
                    Token = token,
                    RefreshToken = refreshToken,
                    User = MapToUserDto(user),
                    ExpiresIn = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60) * 60,
                    RefreshTokenExpiresIn = _configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7) * 24 * 60 * 60
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
        {
            try
            {
                var session = await _authRepository.GetUserSessionAsync(refreshToken);

                if (session == null || session.User == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    };
                }

                var user = session.User;

                // Invalidate old session
                session.Status = "0";
                session.UpdatedOn = DateTime.Now;
                await _authRepository.UpdateUserSessionAsync(session);

                // Generate new tokens
                var newToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();

                // Update user last access
                user.LastAccessedOn = DateTime.Now;
                user.UpdatedOn = DateTime.Now;
                user.UpdatedBy = user.UserName;
                await _authRepository.UpdateUserAsync(user);

                // Create new session
                var newSession = new UserSession
                {
                    UserID = user.RecID,
                    RefreshToken = newRefreshToken,
                    IssuedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7)),
                    DeviceInfo = session.DeviceInfo,
                    IPAddress = ipAddress ?? session.IPAddress,
                    Status = "1",
                    CreatedOn = DateTime.Now,
                    UpdatedOn = DateTime.Now,
                };

                await _authRepository.CreateUserSessionAsync(newSession);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    User = MapToUserDto(user),
                    ExpiresIn = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60) * 60,
                    RefreshTokenExpiresIn = _configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7) * 24 * 60 * 60
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task LogoutAsync(string refreshToken)
        {
            try
            {
                var session = await _authRepository.GetUserSessionAsync(refreshToken);
                if (session != null)
                {
                    session.Status = "0";
                    session.UpdatedOn = DateTime.Now;
                    await _authRepository.UpdateUserSessionAsync(session);

                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<UserDto> CreateUserAsync(CreateUserDto userDto, string createdBy)
        {
            try
            {
                var user = new User
                {
                    BusinessUnit = userDto.BusinessUnit,
                    UserName = userDto.Username,
                    Password = _passwordHasher.HashPassword(userDto.Password),
                    Status = "1",
                    CreatedOn = DateTime.Now,
                    CreatedBy = createdBy,
                    UpdatedOn = DateTime.Now,
                    UpdatedBy = createdBy,
                    LastAccessedOn = DateTime.Now
                };

                var created = await _authRepository.CreateUserAsync(user);
                if (!created)
                    throw new InvalidOperationException($"User {userDto.Username} already exists");

                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ??"");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.System, user.BusinessUnit),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName)
            };


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(
                    _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60)),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.RecID,
                Username = user.UserName,
                BusinessUnit = user.BusinessUnit,
                Status = user.Status,
                LastAccessedOn = user.LastAccessedOn,
                CreatedOn = user.CreatedOn,
                CreatedBy = user.CreatedBy
            };
        }

        public List<string> ValidateAuthRequest(AuthRequestDto request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Request body is required");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(request.BusinessUnit))
                errors.Add("BusinessUnit is required");
            else if (request.BusinessUnit.Length > 4)
                errors.Add("BusinessUnit cannot exceed 4 characters");

            if (string.IsNullOrWhiteSpace(request.Username))
                errors.Add("Username is required");
            else if (request.Username.Length > 40)
                errors.Add("Username cannot exceed 40 characters");

            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required");
            else if (request.Password.Length > 100)
                errors.Add("Password cannot exceed 100 characters");

            return errors;
        }


    }


}