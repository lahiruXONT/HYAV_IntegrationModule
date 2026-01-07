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

public class BusinessUnitRepository : IBusinessUnitRepository
{
    private readonly SystemDbContext _context;
    private readonly ILogger<BusinessUnitRepository> _logger;

    public BusinessUnitRepository(SystemDbContext context,ILogger<BusinessUnitRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<ZYBusinessUnit>> GetAllActiveBusinessUnitsAsync()
    {
        try
        {
            return await _context.BusinessUnits.AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active business units");
            throw;
        }
    }
    public async Task<ZYBusinessUnit?> GetBusinessUnitByCodeAsync(string businessUnitCode)
    {
        if (string.IsNullOrWhiteSpace(businessUnitCode))
            throw new ArgumentException("Business unit code cannot be null or empty", nameof(businessUnitCode));

        try
        {
            return await _context
                .BusinessUnits.AsNoTracking()
                .FirstOrDefaultAsync(bu => bu.BusinessUnit == businessUnitCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business unit by code: {Code}", businessUnitCode);
            throw;
        }
    }
    public async Task<ZYBusinessUnit?> GetBusinessUnitBySalesOrgDivisionAsync(string salesOrg, string division)
    {
        try
        {
            return await _context.BusinessUnits.AsNoTracking().FirstOrDefaultAsync(bu => bu.SalesOrganization == salesOrg && bu.Division == division);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business unit by Sales Organization: {salesOrg}, Division: {Division}", salesOrg, division);
            throw;
        }
    }
    public async Task<bool> BusinessUnitsExistBySalesOrgAsync(string salesOrg)
    {
        try
        {
            return await _context.BusinessUnits.AsNoTracking().AnyAsync(bu => bu.SalesOrganization == salesOrg );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking business units by Sales Organization: {salesOrg}", salesOrg);
            throw;
        }
    }
    public async Task<bool> BusinessUnitExistBySalesOrgDivisionAsync(string salesOrg, string division)
    {
        try
        {
            return await _context.BusinessUnits.AsNoTracking().FirstOrDefaultAsync(bu => bu.Division == division) ?? new BusinessUnitDBMAP { } ;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking business unit by Sales Organization: {salesOrg}, Division: {Division}",salesOrg, division);
            throw;
        }
    }
}
