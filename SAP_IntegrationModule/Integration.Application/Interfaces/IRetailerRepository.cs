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
    Task CreateAsync(Retailer retailer);
    Task UpdateAsync(Retailer retailer);
    Task<TerritoryPostalCode?> GetTerritoryCodeAsync(string postalCode);
    Task<bool> PostalCodeTerritoryExistsAsync(string postalCode);
}
