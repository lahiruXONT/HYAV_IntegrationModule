using Integration.Application.Interfaces;
using Integration.Application.Helpers;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Integration.Infrastructure.Repositories;
public sealed class ProductRepository : IProductRepository, IAsyncDisposable
{
    private readonly UserDbContext _context;
    private IDbContextTransaction? _transaction;

    public ProductRepository( GlobalDbContext globalContext, BusinessUnitResolveHelper businessUnitHelper, ILogger<ProductRepository> logger)
    {
        _globalContext = globalContext ?? throw new ArgumentNullException(nameof(globalContext));
        _businessUnitHelper = businessUnitHelper ?? throw new ArgumentNullException(nameof(businessUnitHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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


                    await transaction.DisposeAsync();
                    await context.DisposeAsync();
                }
            }
            if (_globalTransaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            _buTransactions.Clear();
            _buContexts.Clear();
            _globalTransaction = null;

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

            _buTransactions.Clear();
            _buContexts.Clear();
            _globalContext.ChangeTracker.Clear();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during product transaction rollback");
            throw;
        }
    }

    public async Task<Product?> GetByProductCodeAsync(string productCode, string businessUnit)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Product code cannot be null or empty", nameof(productCode));

        if (string.IsNullOrWhiteSpace(businessUnit))
            throw new ArgumentException("Business unit cannot be null or empty", nameof(businessUnit));


        try
        {
            using var context = await CreateBuDbContextAsync(businessUnit);
            return await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProductCode == productCode && m.BusinessUnit == businessUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by code: {Code} in BU: {BU}", productCode, businessUnit);
            throw;
        }
    }

    public async Task<GlobalProduct?> GetGlobalProductAsync(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Product code cannot be null or empty", nameof(productCode));

        try
        {
            return await _globalContext.GlobalProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.ProductCode == productCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global product by code: {Code}", productCode);
            throw;
        }
    }

    public async Task CreateAsync(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (string.IsNullOrWhiteSpace(product.BusinessUnit))
            throw new ArgumentException("Product business unit cannot be null or empty", nameof(product.BusinessUnit));


        try
        {
            var buContext = await GetBuContextWithTransactionAsync(product.BusinessUnit);

            await UpdateGlobalProductAsync(product);

            buContext.Products.Add(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {Code} in BU: {BU}",
                product.ProductCode, product.BusinessUnit);
            throw;
        }
    }

    public async Task UpdateAsync(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (string.IsNullOrWhiteSpace(product.BusinessUnit))
            throw new ArgumentException("Product business unit cannot be null or empty", nameof(product.BusinessUnit));

        try
        {
            var buContext = await GetBuContextWithTransactionAsync(product.BusinessUnit);

            await UpdateGlobalProductAsync(product);

            var existing = await buContext.Products
                       .FirstAsync(p => p.ProductCode == product.ProductCode
                        && p.BusinessUnit == product.BusinessUnit);

            existing.Status = product.Status;
            existing.UpdatedOn = DateTime.Now;
            existing.UpdatedBy = "SAP_SYNC";
            buContext.Products.Update(existing);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {Code} in BU: {BU}",
                product.ProductCode, product.BusinessUnit);
            throw;
        }
    }

    public async Task UpdateGlobalProductAsync(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));


        try
        {
            var existing = await GetGlobalProductAsync(product.ProductCode);

            if (existing == null)
            {
                var globalProduct = new GlobalProduct
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
                    SortSequence = product.SortSequence,
                    PartAttribute1 = product.PartAttribute1,
                    PartAttribute2 = product.PartAttribute2,
                    Weight = product.Weight,
                    SMMachineType = product.SMMachineType,
                    SMPlatformSize = product.SMPlatformSize,
                    SMCapacity = product.SMCapacity,
                    SMOperatingEnvironment = product.SMOperatingEnvironment,
                    StampingPeriod = product.StampingPeriod,
                    WarrantyPeriod = product.WarrantyPeriod,
                    Status = product.Status,
                    FinishedProduct = product.FinishedProduct,
                    NonStockItemFlag = product.NonStockItemFlag,
                    SalableFlag = product.SalableFlag,
                    BatchProcessingFlag = product.BatchProcessingFlag,
                    BatchControlPrice = product.BatchControlPrice,
                    TaxGroupCode = product.TaxGroupCode,
                    TaxGroupValue = product.TaxGroupValue,
                    CreatedOn = DateTime.Now,
                    UpdatedOn = DateTime.Now,
                    CreatedBy = "SAP_SYNC",
                    UpdatedBy = "SAP_SYNC"
                };
                _globalContext.GlobalProducts.Add(globalProduct);
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
                existing.SortSequence = product.SortSequence;
                existing.PartAttribute1 = product.PartAttribute1;
                existing.PartAttribute2 = product.PartAttribute2;
                existing.Weight = product.Weight;
                existing.SMMachineType = product.SMMachineType;
                existing.SMPlatformSize = product.SMPlatformSize;
                existing.SMCapacity = product.SMCapacity;
                existing.SMOperatingEnvironment = product.SMOperatingEnvironment;
                existing.StampingPeriod = product.StampingPeriod;
                existing.WarrantyPeriod = product.WarrantyPeriod;
                existing.Status = product.Status;
                existing.FinishedProduct = product.FinishedProduct;
                existing.NonStockItemFlag = product.NonStockItemFlag;
                existing.SalableFlag = product.SalableFlag;
                existing.BatchProcessingFlag = product.BatchProcessingFlag;
                existing.BatchControlPrice = product.BatchControlPrice;
                existing.TaxGroupCode = product.TaxGroupCode;
                existing.TaxGroupValue = product.TaxGroupValue;
                existing.UpdatedOn = DateTime.Now;
                existing.UpdatedBy = "SAP_SYNC";
                _globalContext.GlobalProducts.Update(existing);

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global product: {Code}", product.ProductCode);
            throw;
        }
    }

    private async Task<BuDbContext> CreateBuDbContextAsync(string businessUnit)
    {

        var buConfig = await _businessUnitHelper.GetBusinessUnitConfigAsync(businessUnit);

        var optionsBuilder = new DbContextOptionsBuilder<BuDbContext>();
        optionsBuilder.UseSqlServer(buConfig.ConnectionString);

        return new BuDbContext(optionsBuilder.Options, businessUnit);
    }

    private async Task<BuDbContext> GetBuContextWithTransactionAsync(string businessUnit)
    {
        if (!_buContexts.TryGetValue(businessUnit, out var context))
        {
            context = await CreateBuDbContextAsync(businessUnit);
            _buContexts[businessUnit] = context;
        }

        if (!_buTransactions.ContainsKey(businessUnit))
        {
            var transaction = await context.Database.BeginTransactionAsync();
            _buTransactions[businessUnit] = transaction;
        }
    }


    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                if (_globalTransaction != null)
                {
                    await _globalTransaction.DisposeAsync();
                    _globalTransaction = null;
                }

                foreach (var transaction in _buTransactions.Values)
                {
                    if (transaction != null)
                        await transaction.DisposeAsync();
                }
                _buTransactions.Clear();

                foreach (var context in _buContexts.Values)
                {
                    if (context != null)
                        await context.DisposeAsync();
                }
                _buContexts.Clear();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _globalTransaction?.Dispose();
            _globalContext?.Dispose();

            foreach (var transaction in _buTransactions.Values)
            {
                transaction?.Dispose();
            }

            foreach (var context in _buContexts.Values)
            {
                context?.Dispose();
            }

            _buTransactions.Clear();
            _buContexts.Clear();
            _disposed = true;
        }
    }
}
