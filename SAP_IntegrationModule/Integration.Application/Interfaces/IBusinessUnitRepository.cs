using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IBusinessUnitRepository
{
    Task<List<ZYBusinessUnit>> GetAllActiveBusinessUnitsAsync();
    Task<ZYBusinessUnit?> GetBusinessUnitByCodeAsync(string businessUnitCode);
    Task<ZYBusinessUnit?> GetBusinessUnitBySalesOrgDivisionAsync(string salesOrg, string division);
    Task<bool> BusinessUnitsExistBySalesOrgAsync(string salesOrg);
    Task<bool> BusinessUnitExistBySalesOrgDivisionAsync(string salesOrg, string division);
}
