using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IRetailerRepository
{
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<Retailer?> GetByRetailerCodeAsync(string retailerCode, string businessUnit);
    Task<GlobalRetailer?> GetGlobalRetailerAsync(string code);
    Task CreateRetailerAsync(Retailer retailer);
    Task CreateGlobalRetailerAsync(GlobalRetailer retailer);
    Task<TerritoryPostalCode?> GetTerritoryCodeAsync(string postalCode);
    Task<bool> PostalCodeTerritoryExistsAsync(string postalCode);

    Task AddOrUpdateRetailerGeographicDataAsync(
        string businessUnit,
        string retailerCode,
        string postalCode
    );

    Task<string?> GetCurrentPostalCodeForRetailerAsync(string businessUnit, string retailerCode);

    Task ClearGeoCacheAsync();
}
