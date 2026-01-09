using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

public sealed class RetailerRepository : IRetailerRepository
{
    private readonly UserDbContext _context;
    private IDbContextTransaction? _transaction;

    public RetailerRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync() =>
         _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        await _context.SaveChangesAsync();
        await _transaction!.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        _context.ChangeTracker.Clear();
    }

    public Task<Retailer?> GetByRetailerCodeAsync(string code, string bu) =>
        _context.Retailers.FirstOrDefaultAsync(r => r.RetailerCode == code && r.BusinessUnit == bu);

    public Task<GlobalRetailer?> GetGlobalRetailerAsync(string code) =>
        _context.GlobalRetailers.FirstOrDefaultAsync(g => g.RetailerCode == code);

    public Task<TerritoryPostalCode?> GetTerritoryCodeAsync(string postalCode) =>
        _context.TerritoryPostalCodes.FirstOrDefaultAsync(t => t.PostalCode == postalCode);

    public Task<bool> PostalCodeTerritoryExistsAsync(string postalCode) =>
        _context.TerritoryPostalCodes.AnyAsync(t => t.PostalCode == postalCode);

    public async Task CreateRetailerAsync(Retailer retailer)
    {
        await _context.Retailers.AddAsync(retailer);
    }

    public async Task CreateGlobalRetailerAsync(GlobalRetailer retailer)
    {
        await _context.GlobalRetailers.AddAsync(retailer);
    }

}
