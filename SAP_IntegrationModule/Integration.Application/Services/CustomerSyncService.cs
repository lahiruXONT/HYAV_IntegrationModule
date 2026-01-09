using System.Diagnostics;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public sealed class CustomerSyncService : ICustomerSyncService
{
    private readonly IRetailerRepository _customerRepository;
    private readonly ISapClient _sapClient;
    private readonly CustomerMappingHelper _mappingHelper;
    private readonly ILogger<CustomerSyncService> _logger;

    public CustomerSyncService(
        IRetailerRepository customerRepository,
        ISapClient sapClient,
        CustomerMappingHelper mappingHelper,
        ILogger<CustomerSyncService> logger
    )
    {
        _customerRepository =
            customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CustomerSyncResultDto> SyncCustomersFromSapAsync(
        XontCustomerSyncRequestDto request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new CustomerSyncResultDto { SyncDate = DateTime.UtcNow };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Customer",
                    ["RequestDate"] = request.Date,
                }
            )
        )
        {
            try
            {
                var validationErrors = ValidateRequest(request);
                if (validationErrors.Any())
                {
                    result.Success = false;
                    result.Message = $"Validation failed: {string.Join("; ", validationErrors)}";
                    return result;
                }

                _logger.LogInformation("Starting customer sync for date: {Date}", request.Date);

                var dummySapCustomers = new List<SapCustomerResponseDto>
                {
                    new SapCustomerResponseDto
                    {
                        SalesOrganization = "6070", // 4 chars max
                        Distributionchannel = "01", // 2 chars max
                        Division = "67", // 2 chars max
                        Customer = "CUST002", // 15 chars max
                        CustomerName = "Test Customer2", // 75 chars max
                        HouseNo = "123", // 50 chars max
                        Street = "Main St", // 50 chars max
                        Street2 = "Suite A", // 50 chars max
                        Street3 = "",
                        City = "Colombo", // 50 chars max
                        Telephone = "112345678", // 20 chars max
                        Fax = "112345679", // 20 chars max
                        Email = "test@ex.com", // 50 chars max
                        PaymentTerm = "NET", // 3 chars max - FIXED!
                        CreditLimit = 5000.00m,
                        VATRegistrationNumber = "VAT123", // 20 chars max
                        CustomerGroup1 = "GRP", // 10 chars max for RetailerTypeCode
                        CustomerGroup2 = "GRP", // 10 chars max for RetailerClassCode
                        CustomerGroup3 = "GRP", // 10 chars max for RetailerCategoryCode
                        CustomerGroup4 = "",
                        CustomerGroup5 = "",
                        RegionCode = "WE", // Used for TerritoryCode (4 chars max)
                        PostalCode = "CO01", // Used for TerritoryCode - might be too short, consider "HOM1"
                        TodaysDate = DateTime.Now.ToString("yyyyMMdd"),
                    },
                };
                var sapCustomers = dummySapCustomers;
                result.TotalRecords = sapCustomers?.Count ?? 0;

                if (sapCustomers == null || !sapCustomers.Any())
                {
                    result.Success = true;
                    result.Message = "No customer changes found for the specified date";
                    _logger.LogInformation(
                        "No customer changes found for date: {Date}",
                        request.Date
                    );
                    return result;
                }

                _logger.LogInformation(
                    "Retrieved {Count} customer records from SAP",
                    result.TotalRecords
                );

                var groups = sapCustomers
                    .Where(c => !string.IsNullOrWhiteSpace(c.Customer))
                    .GroupBy(c => c.Customer.Trim())
                    .ToList();

                const int batchSize = 100;
                var processedGroups = 0;

                await _customerRepository.BeginTransactionAsync();

                try
                {
                    for (int i = 0; i < groups.Count; i += batchSize)
                    {
                        var batch = groups.Skip(i).Take(batchSize).ToList();
                        _logger.LogDebug(
                            "Processing batch {BatchNumber} of {TotalBatches}",
                            (i / batchSize) + 1,
                            (int)Math.Ceiling((double)groups.Count / batchSize)
                        );

                        foreach (var group in batch)
                        {
                            await ProcessCustomerGroupAsync(group.Key, group.ToList(), result);
                            processedGroups++;
                        }
                    }

                    await _customerRepository.CommitTransactionAsync();
                    await _customerRepository.ClearGeoCacheAsync();
                    result.Success = true;
                    result.Message = BuildSuccessMessage(result);

                    _logger.LogInformation(
                        "Customer sync completed successfully. {Total} processed, {New} new, {Updated} updated, {Skipped} skipped, {Failed} failed in {ElapsedMs}ms",
                        result.TotalRecords,
                        result.NewCustomers,
                        result.UpdatedCustomers,
                        result.SkippedCustomers,
                        result.FailedRecords,
                        stopwatch.ElapsedMilliseconds
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error during customer processing. Processed {Processed}/{Total} groups. Rolling back transaction.",
                        processedGroups,
                        groups.Count
                    );

                    await _customerRepository.RollbackTransactionAsync();

                    result.Success = false;
                    result.Message =
                        $"Sync failed after processing {processedGroups} groups: {ex.Message}";
                    throw;
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = $"SAP API error: {sapEx.Message}";
                _logger.LogError(sapEx, "SAP API error during customer sync");
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Unexpected error: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during customer sync");
                throw new CustomerSyncException($"Customer sync failed: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    private List<string> ValidateRequest(XontCustomerSyncRequestDto request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request cannot be null");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Date))
            errors.Add("Date is required");

        if (!string.IsNullOrWhiteSpace(request.Date))
        {
            if (request.Date.Length != 8)
                errors.Add("Date must be in YYYYMMDD format (8 characters)");

            if (
                !DateTime.TryParseExact(
                    request.Date,
                    "yyyyMMdd",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out _
                )
            )
                errors.Add("Date must be a valid date in YYYYMMDD format");
        }

        return errors;
    }

    private async Task ProcessCustomerGroupAsync(
        string customerCode,
        List<SapCustomerResponseDto> sapCustomers,
        CustomerSyncResultDto result
    )
    {
        if (!sapCustomers.Any())
            return;

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CustomerCode"] = customerCode,
                    ["RecordCount"] = sapCustomers.Count,
                }
            )
        )
        {
            try
            {
                var globalCustomerObj = await _mappingHelper.MapSapToXontGlobalCustomerAsync(
                    sapCustomers[0]
                );

                var globalCustomerExisting = await _customerRepository.GetGlobalRetailerAsync(
                    globalCustomerObj.RetailerCode
                );

                if (globalCustomerExisting == null)
                {
                    await _customerRepository.CreateGlobalRetailerAsync(globalCustomerObj);
                    _logger.LogDebug(
                        "Created global retailer: {RetailerCode}",
                        globalCustomerObj.RetailerCode
                    );
                }
                else if (
                    _mappingHelper.HasGlobalRetailerChanges(
                        globalCustomerExisting,
                        globalCustomerObj
                    )
                )
                {
                    _mappingHelper.UpdateGlobalCustomer(globalCustomerExisting, globalCustomerObj);
                    _logger.LogDebug(
                        "Updated global retailer: {RetailerCode}",
                        globalCustomerObj.RetailerCode
                    );
                }

                foreach (var sapCustomer in sapCustomers)
                {
                    try
                    {
                        var xontRetailer = await _mappingHelper.MapSapToXontCustomerAsync(
                            sapCustomer
                        );

                        var existing = await _customerRepository.GetByRetailerCodeAsync(
                            xontRetailer.RetailerCode,
                            xontRetailer.BusinessUnit
                        );

                        if (existing == null)
                        {
                            await _customerRepository.CreateRetailerAsync(xontRetailer);

                            await _customerRepository.AddOrUpdateRetailerGeographicDataAsync(
                                xontRetailer.BusinessUnit,
                                xontRetailer.RetailerCode,
                                sapCustomer.PostalCode
                            );
                            result.NewCustomers++;
                            _logger.LogDebug(
                                "Created new retailer: {RetailerCode} in BU: {BusinessUnit}",
                                xontRetailer.RetailerCode,
                                xontRetailer.BusinessUnit
                            );
                        }
                        else
                        {
                            var (hasRetailerChanges, hasGeoChanges) =
                                await _mappingHelper.HasRetailerChanges(
                                    existing,
                                    xontRetailer,
                                    sapCustomer.PostalCode
                                );

                            if (hasRetailerChanges || hasGeoChanges)
                            {
                                if (hasRetailerChanges)
                                    _mappingHelper.UpdateCustomer(existing, xontRetailer);

                                if (hasGeoChanges)
                                    await _customerRepository.AddOrUpdateRetailerGeographicDataAsync(
                                        xontRetailer.BusinessUnit,
                                        xontRetailer.RetailerCode,
                                        sapCustomer.PostalCode ?? ""
                                    );
                                result.UpdatedCustomers++;
                                _logger.LogDebug(
                                    "Updated retailer: {RetailerCode} in BU: {BusinessUnit}",
                                    xontRetailer.RetailerCode,
                                    xontRetailer.BusinessUnit
                                );
                            }
                            else
                            {
                                result.SkippedCustomers++;
                                _logger.LogDebug(
                                    "No changes for retailer: {RetailerCode} in BU: {BusinessUnit}",
                                    xontRetailer.RetailerCode,
                                    xontRetailer.BusinessUnit
                                );
                            }
                        }
                    }
                    catch (ValidationExceptionDto valEx)
                    {
                        _logger.LogWarning(
                            "Validation failed for customer {CustomerCode}: {Message}",
                            sapCustomer.Customer,
                            valEx.Message
                        );
                        result.FailedRecords++;
                        result.ValidationErrors ??= new List<string>();
                        result.ValidationErrors.Add(
                            $"Customer {sapCustomer.Customer}: {valEx.Message}"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing customer {CustomerCode} in business unit",
                            sapCustomer.Customer
                        );
                        result.FailedRecords++;
                        throw new CustomerSyncException(
                            $"Failed to process customer {sapCustomer.Customer}: {ex.Message}",
                            sapCustomer.Customer,
                            ex
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing customer group {CustomerCode}",
                    customerCode
                );
                result.FailedRecords += sapCustomers.Count;
                throw;
            }
        }
    }

    private string BuildSuccessMessage(CustomerSyncResultDto result)
    {
        var message = $"Customer sync completed. ";

        if (result.NewCustomers > 0)
            message += $"New: {result.NewCustomers}. ";

        if (result.UpdatedCustomers > 0)
            message += $"Updated: {result.UpdatedCustomers}. ";

        if (result.SkippedCustomers > 0)
            message += $"Skipped: {result.SkippedCustomers}. ";

        if (result.FailedRecords > 0)
            message += $"Failed: {result.FailedRecords}. ";

        message += $"Total processed: {result.TotalRecords} in {result.ElapsedMilliseconds}ms";

        return message;
    }
}
