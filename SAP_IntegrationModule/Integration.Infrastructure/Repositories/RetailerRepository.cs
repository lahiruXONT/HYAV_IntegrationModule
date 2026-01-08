using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

public sealed class RetailerRepository : IRetailerRepository, IAsyncDisposable
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
        try
        {
            await _context.SaveChangesAsync();
            await _transaction!.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            await _transaction!.DisposeAsync();
            _transaction = null;
        }
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

    private Task<GlobalRetailer?> GetGlobalTrackedAsync(string code) =>
        _context.GlobalRetailers.FirstOrDefaultAsync(g => g.RetailerCode == code);

    public Task<TerritoryPostalCode?> GetTerritoryCodeAsync(string postalCode) =>
        _context.TerritoryPostalCodes.FirstOrDefaultAsync(t => t.PostalCode == postalCode);

    public Task<bool> PostalCodeTerritoryExistsAsync(string postalCode) =>
        _context.TerritoryPostalCodes.AnyAsync(t => t.PostalCode == postalCode);

    public async Task CreateAsync(Retailer retailer)
    {
        await UpsertGlobalRetailerAsync(retailer);
        _context.Retailers.Add(retailer);
    }

    public async Task UpdateAsync(Retailer retailer)
    {
        await UpsertGlobalRetailerAsync(retailer);
    }

    private async Task UpsertGlobalRetailerAsync(Retailer retailer)
    {
        var global = await GetGlobalTrackedAsync(retailer.RetailerCode);

        if (global == null)
        {
            _context.GlobalRetailers.Add(
                new GlobalRetailer
                {
                    RetailerCode = retailer.RetailerCode,
                    RetailerName = retailer.RetailerName,
                    AddressLine1 = retailer.AddressLine1,
                    TelephoneNumber = retailer.TelephoneNumber,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    CreatedBy = "SAP_SYNC",
                    UpdatedBy = "SAP_SYNC",
                }
            );
        }
        else
        {
            global.RetailerName = retailer.RetailerName;
            global.AddressLine1 = retailer.AddressLine1;
            global.TelephoneNumber = retailer.TelephoneNumber;
            global.UpdatedOn = DateTime.UtcNow;
            global.UpdatedBy = "SAP_SYNC";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
            await _transaction.DisposeAsync();
    }
}
