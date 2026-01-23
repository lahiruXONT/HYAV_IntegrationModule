using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public sealed class RetailerRepository : IRetailerRepository
{
    private readonly UserDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _geoCacheKeys = new();

    public RetailerRepository(UserDbContext context, IMemoryCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Task<Retailer?> GetByRetailerCodeAsync(string code, string bu) =>
        _context.Retailers.FirstOrDefaultAsync(r => r.RetailerCode == code && r.BusinessUnit == bu);

    public Task<SettlementTerm?> GetSettlementTermAsync(string BusinessUnit, string PaymentTerm) =>
        _context.SettlementTerms.FirstOrDefaultAsync(t =>
            t.BusinessUnit == BusinessUnit
            && t.SourceModuleCode == "RD"
            && t.SAPSettlementTermsCode == PaymentTerm
            && t.Status == "1"
        );

    public async Task<(
        bool hasGeoChanges,
        bool hasDistChannelChanges
    )> CheckClassificationChangesAsync(
        string businessUnit,
        string retailerCode,
        string postalCode,
        string distributionChannel
    )
    {
        var currentClassifications = await _context
            .RetailerClassifications.Where(rc =>
                rc.BusinessUnit == businessUnit
                && rc.RetailerCode == retailerCode
                && rc.Status == "1"
            )
            .ToListAsync();

        var town = currentClassifications
            .FirstOrDefault(rc => rc.MasterGroup == "TOWN")
            ?.MasterGroupValue;

        var distChannel = currentClassifications
            .FirstOrDefault(rc => rc.MasterGroup == "DISTCHNL")
            ?.MasterGroupValue;

        bool hasGeoChanges = town != postalCode;
        bool hasDistChannelChanges = distChannel != distributionChannel;

        return (hasGeoChanges, hasDistChannelChanges);
    }

    public Task<bool> SettlementTermExistsAsync(string BusinessUnit, string PaymentTerm) =>
        _context.SettlementTerms.AnyAsync(t =>
            t.BusinessUnit == BusinessUnit
            && t.SourceModuleCode == "RD"
            && t.SAPSettlementTermsCode == PaymentTerm
            && t.Status == "1"
        );

    public Task<bool> PostalCodeExistsForTownAsync(string businessUnit, string postalCode) =>
        _context.MasterDefinitionValues.AnyAsync(v =>
            v.BusinessUnit == businessUnit
            && v.MasterGroup == "TOWN"
            && v.MasterGroupValue == postalCode
            && v.Status == "1"
        );

    public Task<bool> DistributionChannelExistsAsync(
        string businessUnit,
        string distributionchannel
    ) =>
        _context.MasterDefinitionValues.AnyAsync(v =>
            v.BusinessUnit == businessUnit
            && v.MasterGroup == "DISTCHNL"
            && v.MasterGroupValue == distributionchannel
            && v.Status == "1"
        );

    public async Task CreateRetailerAsync(Retailer retailer)
    {
        await _context.Retailers.AddAsync(retailer);
    }

    public async Task UpdateRetailerAsync(Retailer retailer)
    {
        _context.Retailers.Update(retailer);
    }

    public async Task AddOrUpdateRetailerGeographicDataAsync(
        string businessUnit,
        string retailerCode,
        string postalCode
    )
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            return;
        }

        var existingClassifications = await _context
            .RetailerClassifications.Where(rc =>
                rc.BusinessUnit == businessUnit
                && rc.RetailerCode == retailerCode
                && (
                    rc.MasterGroup == "TOWN"
                    || rc.MasterGroup == "DISTRK"
                    || rc.MasterGroup == "PROVINCE"
                    || rc.MasterGroup == "CONT"
                )
            )
            .ToListAsync();

        if (existingClassifications.Any())
        {
            _context.RetailerClassifications.RemoveRange(existingClassifications);
        }

        var hierarchy = await GetGeographicHierarchySafeAsync(businessUnit, postalCode);
        if (!hierarchy.Any())
            return;

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
                CreatedOn = DateTime.Now,
                CreatedBy = "SAP_SYNC",
                UpdatedOn = DateTime.Now,
                UpdatedBy = "SAP_SYNC",
            })
            .ToList();

        await _context.RetailerClassifications.AddRangeAsync(newClassifications);
    }

    public async Task AddOrUpdateRetailerDistributionChannelAsync(
        string businessUnit,
        string retailerCode,
        string distributionchannel
    )
    {
        if (string.IsNullOrWhiteSpace(distributionchannel))
        {
            return;
        }

        var existingChannels = await _context
            .RetailerClassifications.Where(rc =>
                rc.BusinessUnit == businessUnit
                && rc.RetailerCode == retailerCode
                && rc.MasterGroup == "DISTCHNL"
            )
            .ToListAsync();

        if (existingChannels.Any())
        {
            _context.RetailerClassifications.RemoveRange(existingChannels);
        }

        var distChannelDefinitionValues = await _context
            .MasterDefinitionValues.Where(v =>
                v.BusinessUnit == businessUnit
                && v.MasterGroup == "DISTCHNL"
                && v.MasterGroupValue == distributionchannel
                && v.Status == "1"
            )
            .FirstOrDefaultAsync();

        if (distChannelDefinitionValues == null)
        {
            return;
        }

        var distChannelDefinition = await _context
            .MasterDefinitions.Where(v =>
                v.BusinessUnit == businessUnit && v.MasterGroup == "DISTCHNL" && v.Status == "1"
            )
            .FirstOrDefaultAsync();

        var newChannel = new RetailerClassification
        {
            BusinessUnit = businessUnit,
            RetailerCode = retailerCode,
            MasterGroup = distChannelDefinitionValues.MasterGroup,
            MasterGroupDescription = distChannelDefinition?.GroupDescription ?? "",
            MasterGroupValue = distChannelDefinitionValues.MasterGroupValue,
            MasterGroupValueDescription = distChannelDefinitionValues.MasterGroupValueDescription,
            GroupType = distChannelDefinitionValues.GroupType,
            Status = "1",
            CreatedOn = DateTime.Now,
            CreatedBy = "SAP_SYNC",
            UpdatedOn = DateTime.Now,
            UpdatedBy = "SAP_SYNC",
        };
        await _context.RetailerClassifications.AddAsync(newChannel);
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

    #region Global Retailer Methods(Commented Out)
    //public Task<GlobalRetailer?> GetGlobalRetailerAsync(string code) =>
    //    _context.GlobalRetailers.FirstOrDefaultAsync(g => g.RetailerCode == code);

    //public async Task CreateGlobalRetailerAsync(GlobalRetailer retailer)
    //{
    //    await _context.GlobalRetailers.AddAsync(retailer);
    //}
    //public async Task UpdateGlobalRetailerAsync(GlobalRetailer retailer)
    //{
    //    await _context.GlobalRetailers.Update(retailer);
    //}
    #endregion
}
