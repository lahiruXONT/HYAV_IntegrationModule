using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByProductCodeAsync(string productCode, string businessUnit);
    Task CreateProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    #region Global Product Methods (commented out for now)
    //Task<GlobalProduct?> GetGlobalProductAsync(string productCode);
    //Task CreateGlobalProductAsync(GlobalProduct product);
    //Task UpdateGlobalProductAsync(GlobalProduct product);
    #endregion
}
