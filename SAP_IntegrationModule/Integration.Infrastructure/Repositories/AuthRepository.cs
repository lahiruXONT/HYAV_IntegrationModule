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
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task CreateUserSessionAsync(UserSession session)
    {
        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserSessionAsync(UserSession session)
    {
        _context.UserSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task InvalidateUserSessionsAsync(long userId)
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

    public async Task<bool> CreateUserAsync(User user)
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
}
