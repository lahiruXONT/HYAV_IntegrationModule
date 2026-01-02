using Integration.Application.Interfaces;
using Integration.Application.Helpers;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Integration.Infrastructure.Repositories;

public class ProductRepository : IProductRepository, IAsyncDisposable
{
    public class ProductRepository : IProductRepository, IDisposable
    {
        private readonly GlobalDbContext _globalContext;
        private readonly BusinessUnitResolveHelper _businessUnitHelper;
        private readonly ILogger<ProductRepository> _logger;
        private IDbContextTransaction? _globalTransaction;
        private Dictionary<string, IDbContextTransaction> _buTransactions = new();
        private Dictionary<string, BuDbContext> _buContexts = new();

        {
            _globalContext = globalContext ?? throw new ArgumentNullException(nameof(globalContext));
            _businessUnitHelper = businessUnitHelper ?? throw new ArgumentNullException(nameof(businessUnitHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // TRANSACTION MANAGEMENT
        public async Task BeginTransactionAsync()
        {
            _logger.LogDebug("Beginning global transaction");
            _globalTransaction = await _globalContext.Database.BeginTransactionAsync();
            _buTransactions.Clear();
            _buContexts.Clear();
        }

        public async Task CommitTransactionAsync()
        {
            _logger.LogDebug("Committing transaction");

            try
            {
                var globalChanges = await _globalContext.SaveChangesAsync();
                foreach (var (buCode, context) in _buContexts)
                {
                    if (_buTransactions.TryGetValue(buCode, out var transaction))
                    {
                        var buChanges = await context.SaveChangesAsync();
                        await transaction.CommitAsync();


                        await transaction.DisposeAsync();
                        await context.DisposeAsync();
                    }
                }
                if (_globalTransaction != null)
                {
                    await _globalTransaction.CommitAsync();
                    await _globalTransaction.DisposeAsync();
                }
                _buTransactions.Clear();
                _buContexts.Clear();
                _globalTransaction = null;

            }
            catch (Exception ex)
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RollbackTransactionAsync()
        {

            try
            {
                if (_globalTransaction != null)
                {
                    await _globalTransaction.RollbackAsync();
                    await _globalTransaction.DisposeAsync();
                    _globalTransaction = null;
                }

                foreach (var (buCode, transaction) in _buTransactions)
                {
                    await transaction.RollbackAsync();
                    await transaction.DisposeAsync();

                    if (_buContexts.TryGetValue(buCode, out var context))
                    {
                        await context.DisposeAsync();
                    }
                }

                _buTransactions.Clear();
                _buContexts.Clear();
                _globalContext.ChangeTracker.Clear();

            }
            catch (Exception ex)
            {
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

        {

        if (string.IsNullOrWhiteSpace(product.BusinessUnit))
            throw new ArgumentException("Product business unit cannot be null or empty", nameof(product.BusinessUnit));


            try
            {


            }
            catch (Exception ex)
            {
                throw;
            }
        }

        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(product.BusinessUnit))
                throw new ArgumentException("Product business unit cannot be null or empty", nameof(product.BusinessUnit));


            try
            {
                var buContext = await GetBuContextWithTransactionAsync(product.BusinessUnit);


            }
            catch (Exception ex)
            {
                    product.ProductCode, product.BusinessUnit);
                throw;
            }
        }

        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(product.BusinessUnit))
                throw new ArgumentException("Product business unit cannot be null or empty", nameof(product.BusinessUnit));

            try
            {

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
                    _globalContext.GlobalProducts.Add(globalProduct);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
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

            return context;
        }


        private bool _disposed = false;

        {
            if (!_disposed)
            {
                {

                    foreach (var transaction in _buTransactions.Values)
                    {
                    }
                _buTransactions.Clear();

                    foreach (var context in _buContexts.Values)
                    {
                    }

                    _buTransactions.Clear();
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

        {
        }
    }
}