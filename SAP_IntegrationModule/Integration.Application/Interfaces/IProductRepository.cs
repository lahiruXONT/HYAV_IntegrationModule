using Integration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByProductCodeAsync(string productCode, string businessUnit);
        Task<GlobalProduct?> GetGlobalProductAsync(string productCode);
        Task<List<Product>> GetByBusinessUnitAsync(string businessUnit);
        Task<bool> ExistsAsync(string productCode, string businessUnit);
        Task CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task UpdateGlobalProductAsync(GlobalProduct globalProduct);

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}