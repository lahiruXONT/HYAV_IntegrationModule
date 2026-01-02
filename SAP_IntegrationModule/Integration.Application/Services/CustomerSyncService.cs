using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Integration.Application.Services;


public class CustomerSyncService : ICustomerSyncService
{
    private readonly IRetailerRepository _customerRepository;
    private readonly ISapClient _sapClient;
    private readonly CustomerMappingHelper _mappingHelper;
    private readonly ILogger<CustomerSyncService> _logger;

    public CustomerSyncService(
        IRetailerRepository customerRepository,
        ISapClient sapClient,
        CustomerMappingHelper mappingHelper,
        ILogger<CustomerSyncService> logger)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CustomerSyncResultDto> SyncCustomersFromSapAsync(XontCustomerSyncRequestDto request)
    {
        var result = new CustomerSyncResultDto
        {
            SyncDate = DateTime.Now
        };

        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Date == default)
                throw new ArgumentException("Date is required", nameof(request.Date));

            var sapCustomers = await _sapClient.GetCustomerChangesAsync(request);

            result.TotalRecords = sapCustomers?.Count ?? 0;

            if (sapCustomers == null || !sapCustomers.Any())
            {
                result.Success = true;
                result.Message = "No customer changes found";

                return result;
            }

            result.TotalRecords = sapCustomers.Count;

            var groups = sapCustomers.Where(c => !string.IsNullOrWhiteSpace(c.Customer)).GroupBy(c => c.Customer).ToList();

            await _customerRepository.BeginTransactionAsync();

            try
            {
                foreach (var group in groups)
                {
                    await ProcessCustomerGroupAsync(group.ToList(), result);
                }

                await _customerRepository.CommitTransactionAsync();

                result.Success = true;
                result.Message = $"Customer sync  completed. " +
                               $"Total: {result.TotalRecords}, " +
                               $"New: {result.NewCustomers}, " +
                               $"Updated: {result.UpdatedCustomers}, " +
                               $"Skipped: {result.SkippedCustomers}, ";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer processing in sync , rolling back transaction");

                await _customerRepository.RollbackTransactionAsync();

                result.Success = false;
                result.Message = $"Sync  failed and rolled back: {ex.Message}";
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Sync  failed: {ex.Message}";

            _logger.LogError(ex, "Customer sync  failed");

            throw new CustomerSyncException($"Customer sync failed: {ex.Message}", ex);
        }

        return result;
    }

    private async Task ProcessCustomerGroupAsync(List<SapCustomerResponseDto> sapCustomers,  CustomerSyncResultDto result)
    {
        if (!sapCustomers.Any())
            return;

        var customerCode = sapCustomers[0].Customer;

        foreach (var sapCustomer in sapCustomers)
        {
            try
            {
                var xontRetailer = await _mappingHelper.MapSapToXontCustomerAsync(sapCustomer);

                var existing = await _customerRepository.GetByRetailerCodeAsync(xontRetailer.RetailerCode,xontRetailer.BusinessUnit);

                if (existing == null)
                {
                    xontRetailer.CreatedOn = DateTime.Now;
                    xontRetailer.CreatedBy = "SAP_SYNC";
                    await _customerRepository.CreateAsync(xontRetailer);
                    result.NewCustomers++;
                }
                else
                {
                    if (_mappingHelper.HasChanges(existing, xontRetailer))
                    {
                        _mappingHelper.UpdateCustomer(existing, xontRetailer);
                        existing.UpdatedOn = DateTime.Now;
                        existing.UpdatedBy = "SAP_SYNC";
                        await _customerRepository.UpdateAsync(existing);
                        result.UpdatedCustomers++;
                    }
                    else
                    {
                        result.SkippedCustomers++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing customer {Code} in sync ", sapCustomer.Customer);

                throw new CustomerSyncException( $"Failed to process customer {sapCustomer.Customer}: {ex.Message}", sapCustomer.Customer, ex);
            }
        }

    }

}

