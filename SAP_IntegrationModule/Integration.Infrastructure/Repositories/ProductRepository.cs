using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

public sealed class ProductRepository : IProductRepository, IAsyncDisposable
{
    private readonly UserDbContext _context;
    private IDbContextTransaction? _transaction;

    public ProductRepository(UserDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();

            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        _context.ChangeTracker.Clear();
    }

    public Task<Product?> GetByProductCodeAsync(string productCode, string businessUnit) =>
        _context.Products.FirstOrDefaultAsync(p =>
            p.ProductCode == productCode && p.BusinessUnit == businessUnit
        );

    private Task<GlobalProduct?> GetGlobalTrackedAsync(string productCode) =>
        _context.GlobalProducts.FirstOrDefaultAsync(g => g.ProductCode == productCode);

    public async Task CreateAsync(Product product)
    {
        await UpsertGlobalProductAsync(product);
        _context.Products.Add(product);
    }

    public async Task UpdateAsync(Product product)
    {
        await UpsertGlobalProductAsync(product);
    }

    private async Task UpsertGlobalProductAsync(Product product)
    {
        var existing = await GetGlobalTrackedAsync(product.ProductCode);

        if (existing == null)
        {
            _context.GlobalProducts.Add(
                new GlobalProduct
                {
                    ProductCode = product.ProductCode,
                    Description = product.Description,
                    Description2 = product.Description2,
                    ProductGroup = product.ProductGroup,
                    AlternateSearch = product.AlternateSearch,
                    StockCategory = product.StockCategory,
                    ProductTypeCode = product.ProductTypeCode,
                    UOM1 = product.UOM1,
                    UOM2 = product.UOM2,
                    ConversionFactor = product.ConversionFactor,
                    Status = product.Status,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    CreatedBy = "SAP_SYNC",
                    UpdatedBy = "SAP_SYNC",
                }
            );
        }
        else
        {
            existing.Description = product.Description;
            existing.Description2 = product.Description2;
            existing.ProductGroup = product.ProductGroup;
            existing.AlternateSearch = product.AlternateSearch;
            existing.StockCategory = product.StockCategory;
            existing.ProductTypeCode = product.ProductTypeCode;
            existing.UOM1 = product.UOM1;
            existing.UOM2 = product.UOM2;
            existing.ConversionFactor = product.ConversionFactor;
            existing.Status = product.Status;
            existing.UpdatedOn = DateTime.UtcNow;
            existing.UpdatedBy = "SAP_SYNC";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
            await _transaction.DisposeAsync();
    }
}
