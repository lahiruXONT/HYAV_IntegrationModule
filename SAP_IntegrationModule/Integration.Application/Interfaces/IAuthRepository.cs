using Integration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserAsync(string businessUnit, string username);
    Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
    Task<UserSession?> GetUserSessionAsync(string refreshToken);
    Task UpdateUserAsync(User user);
    Task CreateUserSessionAsync(UserSession session);
    Task UpdateUserSessionAsync(UserSession session);
    Task InvalidateUserSessionsAsync(long userId);
    Task<bool> CreateUserAsync(User user);
    Task<int> GetActiveSessionsCountAsync(long userId);
}
