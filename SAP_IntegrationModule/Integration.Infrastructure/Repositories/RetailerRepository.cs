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

namespace Integration.Infrastructure.Repositories
{

    public class RetailerRepository : IRetailerRepository
    {
        private readonly GlobalDbContext _globalContext;
        private readonly BusinessUnitResolveHelper _businessUnitHelper;
        private readonly ILogger<RetailerRepository> _logger;
        private IDbContextTransaction? _globalTransaction;
        private Dictionary<string, IDbContextTransaction> _buTransactions = new();
        private Dictionary<string, BuDbContext> _buContexts = new();

        public RetailerRepository(
            GlobalDbContext globalContext,
            BusinessUnitResolveHelper businessUnitHelper,
            ILogger<RetailerRepository> logger)
        {
            _globalContext = globalContext ?? throw new ArgumentNullException(nameof(globalContext));
            _businessUnitHelper = businessUnitHelper ?? throw new ArgumentNullException(nameof(businessUnitHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task BeginTransactionAsync()
        {
            _globalTransaction = await _globalContext.Database.BeginTransactionAsync();
            _buTransactions.Clear();
            _buContexts.Clear();
        }

        public async Task CommitTransactionAsync()
        {

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
                _logger.LogError(ex, "Error committing retailer transaction");
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
                _logger.LogError(ex, "Error during retailer transaction rollback");
                throw;
            }
        }

        public async Task<Retailer?> GetByRetailerCodeAsync(string retailerCode, string businessUnit)
        {
            if (string.IsNullOrWhiteSpace(retailerCode))
                throw new ArgumentException("Retailer code cannot be null or empty", nameof(retailerCode));

            if (string.IsNullOrWhiteSpace(businessUnit))
                throw new ArgumentException("Business unit cannot be null or empty", nameof(businessUnit));


            try
            {
                using var context = await CreateBuDbContextAsync(businessUnit);
                return await context.Retailers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RetailerCode == retailerCode && r.BusinessUnit == businessUnit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retailer by code: {Code} in BU: {BU}", retailerCode, businessUnit);
                throw;
            }
        }

        public async Task<GlobalRetailer?> GetGlobalRetailerAsync(string retailerCode)
        {
            if (string.IsNullOrWhiteSpace(retailerCode))
                throw new ArgumentException("Retailer code cannot be null or empty", nameof(retailerCode));


            try
            {
                return await _globalContext.GlobalRetailers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.RetailerCode == retailerCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global retailer by code: {Code}", retailerCode);
                throw;
            }
        }

        public async Task CreateAsync(Retailer retailer)
        {
            if (retailer == null)
                throw new ArgumentNullException(nameof(retailer));

            if (string.IsNullOrWhiteSpace(retailer.BusinessUnit))
                throw new ArgumentException("Retailer business unit cannot be null or empty", nameof(retailer.BusinessUnit));


            try
            {
                var buContext = await GetBuContextWithTransactionAsync(retailer.BusinessUnit);

                await UpdateGlobalRetailerAsync(new GlobalRetailer
                {
                    RetailerCode = retailer.RetailerCode,
                    RetailerName = retailer.RetailerName,
                    AddressLine1 = retailer.AddressLine1,
                    AddressLine2 = retailer.AddressLine2,
                    AddressLine3 = retailer.AddressLine3,
                    AddressLine4 = retailer.AddressLine4,
                    AddressLine5 = retailer.AddressLine5,
                    TelephoneNumber = retailer.TelephoneNumber,
                    FaxNumber = retailer.FaxNumber,
                    EmailAddress = retailer.EmailAddress,
                    //SalesOrganization = retailer.SalesOrganization,
                    //DistributionChannel = retailer.DistributionChannel,
                    //Plant = retailer.Plant,
                    //CustomerGroup = retailer.CustomerGroup,
                    //PaymentTerms = retailer.PaymentTerms,
                    //VATRegistrationNo = retailer.VATRegistrationNo,
                    //TaxCode = retailer.TaxCode,
                    //Status = retailer.Status,
                    CreatedOn = DateTime.Now,
                    UpdatedOn = DateTime.Now,
                    CreatedBy = "SAP_SYNC",
                    UpdatedBy = "SAP_SYNC"
                });

                buContext.Retailers.Add(retailer);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating retailer: {Code} in BU: {BU}",
                    retailer.RetailerCode, retailer.BusinessUnit);
                throw;
            }
        }

        public async Task UpdateAsync(Retailer retailer)
        {
            if (retailer == null)
                throw new ArgumentNullException(nameof(retailer));

            if (string.IsNullOrWhiteSpace(retailer.BusinessUnit))
                throw new ArgumentException("Retailer business unit cannot be null or empty", nameof(retailer.BusinessUnit));


            try
            {
                var buContext = await GetBuContextWithTransactionAsync(retailer.BusinessUnit);

                await UpdateGlobalRetailerAsync(new GlobalRetailer
                {
                    RetailerCode = retailer.RetailerCode,
                    RetailerName = retailer.RetailerName,
                    AddressLine1 = retailer.AddressLine1,
                    AddressLine2 = retailer.AddressLine2,
                    AddressLine3 = retailer.AddressLine3,
                    AddressLine4 = retailer.AddressLine4,
                    AddressLine5 = retailer.AddressLine5,
                    TelephoneNumber = retailer.TelephoneNumber,
                    FaxNumber = retailer.FaxNumber,
                    EmailAddress = retailer.EmailAddress,
                    //SalesOrganization = retailer.SalesOrganization,
                    //DistributionChannel = retailer.DistributionChannel,
                    //Plant = retailer.Plant,
                    //CustomerGroup = retailer.CustomerGroup,
                    //PaymentTerms = retailer.PaymentTerms,
                    //VATRegistrationNo = retailer.VATRegistrationNo,
                    //TaxCode = retailer.TaxCode,
                    //Status = retailer.Status,
                    UpdatedOn = DateTime.Now,
                    UpdatedBy = "SAP_SYNC"
                });

                buContext.Retailers.Update(retailer);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating retailer: {Code} in BU: {BU}",
                    retailer.RetailerCode, retailer.BusinessUnit);
                throw;
            }
        }

        public async Task UpdateGlobalRetailerAsync(GlobalRetailer globalRetailer)
        {
            if (globalRetailer == null)
                throw new ArgumentNullException(nameof(globalRetailer));


            try
            {
                var existing = await GetGlobalRetailerAsync(globalRetailer.RetailerCode);

                if (existing == null)
                {
                    _globalContext.GlobalRetailers.Add(globalRetailer);
                }
                else
                {
                    _globalContext.Attach(existing);
                    _globalContext.Entry(existing).CurrentValues.SetValues(globalRetailer);

                    existing.RetailerName = globalRetailer.RetailerName;
                    existing.AddressLine1 = globalRetailer.AddressLine1;
                    existing.AddressLine2 = globalRetailer.AddressLine2;
                    existing.AddressLine3 = globalRetailer.AddressLine3;
                    existing.AddressLine4 = globalRetailer.AddressLine4;
                    existing.AddressLine5 = globalRetailer.AddressLine5;
                    existing.TelephoneNumber = globalRetailer.TelephoneNumber;
                    existing.FaxNumber = globalRetailer.FaxNumber;
                    existing.EmailAddress = globalRetailer.EmailAddress;
                    //existing.SalesOrganization = globalRetailer.SalesOrganization;
                    //existing.DistributionChannel = globalRetailer.DistributionChannel;
                    //existing.Plant = globalRetailer.Plant;
                    //existing.CustomerGroup = globalRetailer.CustomerGroup;
                    //existing.PaymentTerms = globalRetailer.PaymentTerms;
                    //existing.VATRegistrationNo = globalRetailer.VATRegistrationNo;
                    //existing.TaxCode = globalRetailer.TaxCode;
                    //existing.Status = globalRetailer.Status;
                    existing.UpdatedOn = globalRetailer.UpdatedOn;
                    existing.UpdatedBy = globalRetailer.UpdatedBy;

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating global retailer: {Code}", globalRetailer.RetailerCode);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _globalTransaction?.Dispose();

                    foreach (var transaction in _buTransactions.Values)
                    {
                        transaction.Dispose();
                    }

                    foreach (var context in _buContexts.Values)
                    {
                        context.Dispose();
                    }

                    _buTransactions.Clear();
                    _buContexts.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}