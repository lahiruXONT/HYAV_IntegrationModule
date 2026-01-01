using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs
{
    public class AuthRequestDto
    {
        public string BusinessUnit { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public int ExpiresIn { get; set; }
        public int RefreshTokenExpiresIn { get; set; }
    }

    public class UserDto
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string BusinessUnit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastAccessedOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateUserDto
    {
        public string BusinessUnit { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

}
