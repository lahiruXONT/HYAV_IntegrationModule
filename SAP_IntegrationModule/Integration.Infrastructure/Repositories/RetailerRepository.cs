using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
namespace Integration.Infrastructure.Repositories;

public sealed class RetailerRepository : IRetailerRepository, IAsyncDisposable
{
    private readonly GlobalDbContext _globalContext;
    private readonly BusinessUnitResolveHelper _businessUnitHelper;
    private readonly ILogger<RetailerRepository> _logger;
    private IDbContextTransaction? _globalTransaction;
    private Dictionary<string, IDbContextTransaction> _buTransactions = new();
    private Dictionary<string, BuDbContext> _buContexts = new();

    public RetailerRepository(GlobalDbContext globalContext,BusinessUnitResolveHelper businessUnitHelper, ILogger<RetailerRepository> logger)
    {
        _globalContext = globalContext ?? throw new ArgumentNullException(nameof(globalContext));
        _businessUnitHelper = businessUnitHelper ?? throw new ArgumentNullException(nameof(businessUnitHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    

    public async Task BeginTransactionAsync()
        => _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        try
        {
            var globalChanges = await _globalContext.SaveChangesAsync();

            foreach (var (buCode, context) in _buContexts)
            {
                if (_buTransactions.TryGetValue(buCode, out var transaction))
                {
                    var buChanges = await context.SaveChangesAsync();
                    await transaction.CommitAsync();


                    await transaction.DisposeAsync();
                    await context.DisposeAsync();
                }
            }

            if (_globalTransaction != null)
            {
                await _globalTransaction.CommitAsync();
                await _globalTransaction.DisposeAsync();
            }
            _buTransactions.Clear();
            _buContexts.Clear();
            _globalTransaction = null;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing retailer transaction");
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

            _buTransactions.Clear();
            _buContexts.Clear();
            _globalContext.ChangeTracker.Clear();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during retailer transaction rollback");
            throw;
        }
    }

    public async Task<Retailer?> GetByRetailerCodeAsync(string retailerCode, string businessUnit)
    {
        if (string.IsNullOrWhiteSpace(retailerCode))
            throw new ArgumentException("Retailer code cannot be null or empty", nameof(retailerCode));

        if (string.IsNullOrWhiteSpace(businessUnit))
            throw new ArgumentException("Business unit cannot be null or empty", nameof(businessUnit));


        try
        {
            using var context = await CreateBuDbContextAsync(businessUnit);
            return await context.Retailers
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RetailerCode == retailerCode && r.BusinessUnit == businessUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting retailer by code: {Code} in BU: {BU}", retailerCode, businessUnit);
            throw;
        }
    }

    public async Task<GlobalRetailer?> GetGlobalRetailerAsync(string retailerCode)
    {
        if (string.IsNullOrWhiteSpace(retailerCode))
            throw new ArgumentException("Retailer code cannot be null or empty", nameof(retailerCode));


        try
        {
            return await _globalContext.GlobalRetailers
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.RetailerCode == retailerCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global retailer by code: {Code}", retailerCode);
            throw;
        }
    }

    public async Task CreateAsync(Retailer retailer)
    {
        if (retailer == null)
            throw new ArgumentNullException(nameof(retailer));

        if (string.IsNullOrWhiteSpace(retailer.BusinessUnit))
            throw new ArgumentException("Retailer business unit cannot be null or empty", nameof(retailer.BusinessUnit));


        try
        {
            var buContext = await GetBuContextWithTransactionAsync(retailer.BusinessUnit);

            await UpdateGlobalRetailerAsync(retailer);

            buContext.Retailers.Add(retailer);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating retailer: {Code} in BU: {BU}",
                retailer.RetailerCode, retailer.BusinessUnit);
            throw;
        }
    }

    public async Task UpdateAsync(Retailer retailer)
    {
        if (retailer == null)
            throw new ArgumentNullException(nameof(retailer));

        if (string.IsNullOrWhiteSpace(retailer.BusinessUnit))
            throw new ArgumentException("Retailer business unit cannot be null or empty", nameof(retailer.BusinessUnit));


        try
        {
            var buContext = await GetBuContextWithTransactionAsync(retailer.BusinessUnit);

            await UpdateGlobalRetailerAsync(retailer);

            var existing = await buContext.Retailers
                        .FirstAsync(r => r.RetailerCode == retailer.RetailerCode
                         && r.BusinessUnit == retailer.BusinessUnit);
            
            existing.TerritoryCode = retailer.TerritoryCode;
            existing.CreditLimit = retailer.CreditLimit;
            existing.Status = retailer.Status;
            existing.UpdatedOn = DateTime.Now;
            existing.UpdatedBy = "SAP_SYNC";
            buContext.Retailers.Update(existing);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating retailer: {Code} in BU: {BU}",
                retailer.RetailerCode, retailer.BusinessUnit);
            throw;
        }
    }
    public async Task UpdateGlobalRetailerAsync(Retailer retailer)
    {
        if (retailer == null)
            throw new ArgumentNullException(nameof(retailer));

    private async Task UpdateOrInsertGlobalRetailerAsync(Retailer retailer)
    {
        var global = await GetGlobalTrackedAsync(retailer.RetailerCode);

            if (existing == null)
            {
                var globalRetailer = new GlobalRetailer
                {
                    RetailerCode = retailer.RetailerCode,
                    RetailerName = retailer.RetailerName,
                    AddressLine1 = retailer.AddressLine1,
                    AddressLine2 = retailer.AddressLine2,
                    AddressLine3 = retailer.AddressLine3,
                    AddressLine4 = retailer.AddressLine4,
                    AddressLine5 = retailer.AddressLine5,
                    TelephoneNumber = retailer.TelephoneNumber,
                    FaxNumber = retailer.FaxNumber,
                    EmailAddress = retailer.EmailAddress,
                    TerritoryCode = retailer.TerritoryCode,

                    CreatedOn = DateTime.Now,
                    UpdatedOn = DateTime.Now,
                    CreatedBy = "SAP_SYNC",
                    UpdatedBy = "SAP_SYNC"
                };

                _globalContext.GlobalRetailers.Add(globalRetailer);
            }
            else
            {
                existing.RetailerName = retailer.RetailerName;
                existing.AddressLine1 = retailer.AddressLine1;
                existing.AddressLine2 = retailer.AddressLine2;
                existing.AddressLine3 = retailer.AddressLine3;
                existing.AddressLine4 = retailer.AddressLine4;
                existing.AddressLine5 = retailer.AddressLine5;
                existing.TelephoneNumber = retailer.TelephoneNumber;
                existing.FaxNumber = retailer.FaxNumber;
                existing.EmailAddress = retailer.EmailAddress;
                existing.TerritoryCode = retailer.TerritoryCode;

                existing.UpdatedOn = DateTime.Now;
                existing.UpdatedBy = "SAP_SYNC";
                _globalContext.GlobalRetailers.Update(existing);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error updating GlobalRetailer | RetailerCode: {RetailerCode}",retailer.RetailerCode
            );

            throw;
        }
    }
    public async Task<bool>PostalCodeTerritoryExistsAsync(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be null or empty", nameof(postalCode));

        try
        {
            return await _globalContext.TerritoryPostalCodes.AsNoTracking().AnyAsync(t => t.PostalCode == postalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting territory by postal code: {Code}", postalCode);
            throw;
        }

    }
    public async Task<TerritoryPostalCode?> GetTerritoryCodeAsync(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be null or empty", nameof(postalCode));

        try
        {
            return await _globalContext.TerritoryPostalCodes.AsNoTracking().FirstOrDefaultAsync(t => t.PostalCode == postalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting territory by postal code: {Code}", postalCode);
            throw;
        }
    }

    public async Task<List<TerritoryPostalCode>> GetAllTerritoryCodeAsync()
    {

        try
        {
            return await _globalContext.TerritoryPostalCodes.AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting territory with postal codes");
            throw;
        }
    }

    private async Task<BuDbContext> CreateBuDbContextAsync(string businessUnit)
    {

        var buConfig = await _businessUnitHelper.GetBusinessUnitConfigAsync(businessUnit);

        var optionsBuilder = new DbContextOptionsBuilder<BuDbContext>();
        optionsBuilder.UseSqlServer(buConfig.ConnectionString);

        return new BuDbContext(optionsBuilder.Options, businessUnit);
    }

    private async Task<BuDbContext> GetBuContextWithTransactionAsync(string businessUnit)
    {
        if (!_buContexts.TryGetValue(businessUnit, out var context))
        {
            context = await CreateBuDbContextAsync(businessUnit);
            _buContexts[businessUnit] = context;
        }

        if (!_buTransactions.ContainsKey(businessUnit))
        {
            var transaction = await context.Database.BeginTransactionAsync();
            _buTransactions[businessUnit] = transaction;
        }

        return context;
    }

    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                if (_globalTransaction != null)
                {
                    await _globalTransaction.DisposeAsync();
                    _globalTransaction = null;
                }

                foreach (var transaction in _buTransactions.Values)
                {
                    if (transaction != null)
                        await transaction.DisposeAsync();
                }
                _buTransactions.Clear();

                foreach (var context in _buContexts.Values)
                {
                    if (context != null)
                        await context.DisposeAsync();
                }
                _buContexts.Clear();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _globalTransaction?.Dispose();
            _globalContext?.Dispose();

            foreach (var transaction in _buTransactions.Values)
            {
                transaction?.Dispose();
            }

            foreach (var context in _buContexts.Values)
            {
                context?.Dispose();
            }

            _buTransactions.Clear();
            _buContexts.Clear();
            _disposed = true;
        }
    }
}
