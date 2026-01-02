using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Helpers;

public class PasswordHashHelper
{
    private readonly ILogger<PasswordHashHelper> _logger;

    public PasswordHashHelper(ILogger<PasswordHashHelper> logger)
    {
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);

            var builder = new StringBuilder();
            foreach (var b in hash)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash password");
            throw;
        }
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var hashedInput = HashPassword(password);
            if (hashedInput == hashedPassword)
                return true;               

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify password");
            return false;
        }
    }
}