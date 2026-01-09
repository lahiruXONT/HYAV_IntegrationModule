using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

public sealed class ProductRepository : IProductRepository
{
    private readonly UserDbContext _context;
    private IDbContextTransaction? _transaction;

    public ProductRepository(UserDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task BeginTransactionAsync() =>
        _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        await _context.SaveChangesAsync();
        await _transaction!.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
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

    public Task<GlobalProduct?> GetGlobalProductAsync(string productCode) =>
        _context.GlobalProducts.FirstOrDefaultAsync(g => g.ProductCode == productCode);

    public async Task CreateProductAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task CreateGlobalProductAsync(GlobalProduct product)
    {
        await _context.GlobalProducts.AddAsync(product);
    }
}
