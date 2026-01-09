using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public sealed class RetailerRepository : IRetailerRepository
{
    private readonly UserDbContext _context;
    private IDbContextTransaction? _transaction;
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _geoCacheKeys = new();

    public RetailerRepository(UserDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
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

    public Task<string?> GetCurrentPostalCodeForRetailerAsync(
        string businessUnit,
        string retailerCode
    ) =>
        _context
            .RetailerClassifications.Where(rc =>
                rc.BusinessUnit == businessUnit
                && rc.RetailerCode == retailerCode
                && rc.MasterGroup == "TOWN"
                && rc.Status == "1"
            )
            .Select(rc => rc.MasterGroupValue)
            .FirstOrDefaultAsync();

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

    public async Task AddOrUpdateRetailerGeographicDataAsync(
        string businessUnit,
        string retailerCode,
        string postalCode
    )
    {
        var hierarchy = await GetGeographicHierarchySafeAsync(businessUnit, postalCode);
        if (!hierarchy.Any())
            return;

        var existingClassifications = await _context
            .RetailerClassifications.Where(rc =>
                rc.BusinessUnit == businessUnit
                && rc.RetailerCode == retailerCode
                && rc.Status == "1"
            )
            .ToListAsync();

        foreach (var rc in existingClassifications)
        {
            rc.Status = "0";
            rc.UpdatedOn = DateTime.UtcNow;
            rc.UpdatedBy = "SAP_SYNC";
        }

        var masterGroups = hierarchy.Select(h => h.MasterGroup).Distinct().ToList();

        var masterDefinitions = (
            await _context
                .MasterDefinitions.Where(d => d.BusinessUnit == businessUnit && d.Status == "1")
                .AsNoTracking()
                .ToListAsync()
        )
            .Where(d => masterGroups.Contains(d.MasterGroup))
            .GroupBy(d => d.MasterGroup)
            .ToDictionary(g => g.Key, g => g.First().GroupDescription);

        var newClassifications = hierarchy
            .Select(h => new RetailerClassification
            {
                BusinessUnit = businessUnit,
                RetailerCode = retailerCode,
                MasterGroup = h.MasterGroup,
                MasterGroupDescription =
                    masterDefinitions.GetValueOrDefault(h.MasterGroup) ?? h.MasterGroup,
                MasterGroupValue = h.MasterGroupValue,
                MasterGroupValueDescription = h.MasterGroupValueDescription,
                GroupType = h.GroupType,
                Status = "1",
                CreatedOn = DateTime.UtcNow,
                CreatedBy = "SAP_SYNC",
                UpdatedOn = DateTime.UtcNow,
                UpdatedBy = "SAP_SYNC",
            })
            .ToList();

        await _context.RetailerClassifications.AddRangeAsync(newClassifications);
    }

    private async Task<List<MasterDefinitionValue>> GetGeographicHierarchySafeAsync(
        string businessUnit,
        string postalCode
    )
    {
        var cacheKey = $"GeoClassifications:{businessUnit}:{postalCode}";
        if (_cache.TryGetValue(cacheKey, out List<MasterDefinitionValue>? cached))
            return cached ?? new List<MasterDefinitionValue>();

        var allValues = new List<MasterDefinitionValue>();
        var queue = new Queue<MasterDefinitionValue>();

        var initial = await _context
            .MasterDefinitionValues.Where(v =>
                v.BusinessUnit == businessUnit
                && v.MasterGroup == "TOWN"
                && v.MasterGroupValue == postalCode
                && v.Status == "1"
            )
            .AsNoTracking()
            .ToListAsync();

        foreach (var v in initial)
            queue.Enqueue(v);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (
                !allValues.Any(v =>
                    v.MasterGroup == current.MasterGroup
                    && v.MasterGroupValue == current.MasterGroupValue
                )
            )
                allValues.Add(current);

            if (
                !string.IsNullOrWhiteSpace(current.ParentMasterGroup)
                && !string.IsNullOrWhiteSpace(current.ParentMasterGroupValue)
            )
            {
                var parent = await _context
                    .MasterDefinitionValues.Where(v =>
                        v.BusinessUnit == businessUnit
                        && v.MasterGroup == current.ParentMasterGroup
                        && v.MasterGroupValue == current.ParentMasterGroupValue
                        && v.Status == "1"
                    )
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var p in parent)
                {
                    if (
                        !allValues.Any(v =>
                            v.MasterGroup == p.MasterGroup
                            && v.MasterGroupValue == p.MasterGroupValue
                        )
                    )
                        queue.Enqueue(p);
                }
            }
        }

        _cache.Set(cacheKey, allValues);
        _geoCacheKeys.Add(cacheKey);
        return allValues;
    }

    public Task ClearGeoCacheAsync()
    {
        foreach (var key in _geoCacheKeys.ToList())
        {
            _cache.Remove(key);
            _geoCacheKeys.Remove(key);
        }

        return Task.CompletedTask;
    }
}
