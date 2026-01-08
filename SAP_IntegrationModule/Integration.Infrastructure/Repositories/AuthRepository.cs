using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Integration.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(UserDbContext context, ILogger<AuthRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<User?> GetUserAsync(string businessUnit, string username) =>
        _context
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.BusinessUnit == businessUnit && u.UserName == username && u.Status == "1"
            );

    public Task<User?> GetUserByRefreshTokenAsync(string refreshToken) =>
        _context
            .Users.Include(u => u.Sessions)
            .FirstOrDefaultAsync(u =>
                u.Sessions != null
                && u.Sessions.Any(s =>
                    s.RefreshToken == refreshToken && s.Status == "1" && s.ExpiresAt > DateTime.Now
                )
            );

    public Task<UserSession?> GetUserSessionAsync(string refreshToken) =>
        _context
            .UserSessions.Include(s => s.User)
            .FirstOrDefaultAsync(s =>
                s.RefreshToken == refreshToken && s.Status == "1" && s.ExpiresAt > DateTime.Now
            );

    public Task<int> GetActiveSessionsCountAsync(long userId) =>
        _context.UserSessions.CountAsync(s =>
            s.UserID == userId && s.Status == "1" && s.ExpiresAt > DateTime.Now
        );

    public async Task UpdateUserAsync(User user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {Username}", user.UserName);
            throw;
        }
    }

    public async Task CreateUserSessionAsync(UserSession session)
    {
        try
        {
            await _context.UserSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user session for user {UserId}", session.UserID);
            throw;
        }
    }

    public async Task UpdateUserSessionAsync(UserSession session)
    {
        try
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user session {SessionId}", session.RecID);
            throw;
        }
    }

    public async Task InvalidateUserSessionsAsync(long userId)
    {
        try
        {
            var sessions = await _context
                .UserSessions.Where(s => s.UserID == userId && s.Status == "1")
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.Status = "0";
                session.UpdatedOn = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        try
        {
            var existing = await _context.Users.AnyAsync(u =>
                u.BusinessUnit == user.BusinessUnit && u.UserName == user.UserName
            );

            if (existing)
                return false;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {Username}", user.UserName);
            throw;
        }
    }
}
