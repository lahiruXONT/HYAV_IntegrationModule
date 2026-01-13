using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IProductRepository
{
    //Task BeginTransactionAsync();
    //Task CommitTransactionAsync();
    //Task RollbackTransactionAsync();
    Task<Product?> GetByProductCodeAsync(string productCode, string businessUnit);
    Task<GlobalProduct?> GetGlobalProductAsync(string productCode);
    Task CreateProductAsync(Product product);
    Task CreateGlobalProductAsync(GlobalProduct product);
}
