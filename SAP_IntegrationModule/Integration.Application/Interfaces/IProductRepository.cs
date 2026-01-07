using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByProductCodeAsync(string productCode, string businessUnit);
    Task CreateAsync(Product product);
    Task UpdateAsync(Product product);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
