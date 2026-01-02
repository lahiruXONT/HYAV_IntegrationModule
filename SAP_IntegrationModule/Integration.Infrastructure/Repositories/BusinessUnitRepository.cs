using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Infrastructure.Repositories;

public class BusinessUnitRepository : IBusinessUnitRepository
{
    private readonly GlobalDbContext _context;
    private readonly ILogger<BusinessUnitRepository> _logger;

    public BusinessUnitRepository(GlobalDbContext context,ILogger<BusinessUnitRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<BusinessUnitDBMAP>> GetAllActiveBusinessUnitsAsync()
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

    public async Task<BusinessUnitDBMAP> GetBusinessUnitByCodeAsync(string businessUnitCode)
    {
        if (string.IsNullOrWhiteSpace(businessUnitCode))
            throw new ArgumentException("Business unit code cannot be null or empty", nameof(businessUnitCode));

        try
        {
            return await _context.BusinessUnits.AsNoTracking().FirstOrDefaultAsync(bu => bu.BusinessUnit == businessUnitCode) ?? new BusinessUnitDBMAP { };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business unit by code: {Code}", businessUnitCode);
            throw;
        }
    }

    public async Task<BusinessUnitDBMAP> GetBusinessUnitByDivisionAsync(string division)
    {
        if (string.IsNullOrWhiteSpace(division))
            throw new ArgumentException("Division cannot be null or empty", nameof(division));

        try
        {
            return await _context.BusinessUnits.AsNoTracking().FirstOrDefaultAsync(bu => bu.Division == division) ?? new BusinessUnitDBMAP { } ;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business unit by Division: {Division}", division);
            throw;
        }
    }
}