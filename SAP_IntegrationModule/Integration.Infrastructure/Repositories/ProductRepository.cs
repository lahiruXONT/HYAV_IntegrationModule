using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public sealed class ProductRepository : IProductRepository
{
    private readonly UserDbContext _context;

    public ProductRepository(UserDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Product?> GetByProductCodeAsync(string productCode, string businessUnit) =>
        _context.Products.FirstOrDefaultAsync(p =>
            p.ProductCode == productCode && p.BusinessUnit == businessUnit
        );

    public async Task CreateProductAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateProductAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }
    #region Global Product Methods(Commented Out)
    //public Task<GlobalProduct?> GetGlobalProductAsync(string productCode) =>
    //    _context.GlobalProducts.FirstOrDefaultAsync(g => g.ProductCode == productCode);
    //public async Task CreateGlobalProductAsync(GlobalProduct product)
    //{
    //    await _context.GlobalProducts.AddAsync(product);
    //    await _context.SaveChangesAsync();
    //}

    //public async Task UpdateGlobalProductAsync(GlobalProduct product)
    //{
    //    _context.GlobalProducts.Update(product);
    //    await _context.SaveChangesAsync();
    //}
    #endregion
}
