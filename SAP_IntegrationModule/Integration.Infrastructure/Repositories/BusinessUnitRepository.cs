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

    public BusinessUnitRepository(SystemDbContext context, ILogger<BusinessUnitRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<List<ZYBusinessUnit>> GetAllActiveBusinessUnitsAsync() =>
        _context.BusinessUnits.AsNoTracking().ToListAsync();

    public Task<ZYBusinessUnit?> GetBusinessUnitBySalesOrgDivisionAsync(
        string salesOrg,
        string division
    ) =>
        _context
            .BusinessUnits.AsNoTracking()
            .FirstOrDefaultAsync(bu => bu.SalesOrganization == salesOrg && bu.Division == division);

    public Task<bool> BusinessUnitsExistBySalesOrgAsync(string salesOrg) =>
        _context.BusinessUnits.AsNoTracking().AnyAsync(bu => bu.SalesOrganization == salesOrg);

    public Task<bool> BusinessUnitExistBySalesOrgDivisionAsync(string salesOrg, string division) =>
        _context
            .BusinessUnits.AsNoTracking()
            .AnyAsync(bu => bu.SalesOrganization == salesOrg && bu.Division == division);

    public Task<ZYBusinessUnit?> GetBusinessUnitByCodeAsync(string businessUnitCode) =>
        _context
            .BusinessUnits.AsNoTracking()
            .FirstOrDefaultAsync(bu => bu.BusinessUnit == businessUnitCode);
}
