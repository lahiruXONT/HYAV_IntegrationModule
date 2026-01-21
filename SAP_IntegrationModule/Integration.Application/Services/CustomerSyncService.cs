using System.Diagnostics;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
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
                    result.Message =
                        $"Customer sync request Validation failed: {string.Join("; ", validationErrors)}";
                    _logger.LogWarning(result.Message);
                    return result;
                }

                _logger.LogInformation("Starting customer sync for date: {Date}", request.Date);

                var sapCustomers = await _sapClient.GetCustomerChangesAsync(request);

                result.TotalRecords = sapCustomers?.Count ?? 0;

                if (sapCustomers == null || !sapCustomers.Any())
                {
                    result.Success = true;
                    result.Message = $"No customer changes found for  date: {request.Date}";
                    _logger.LogInformation(result.Message);
                    return result;
                }

                _logger.LogInformation(
                    "Retrieved {Count} customer records from SAP",
                    result.TotalRecords
                );

                var customerGroups = sapCustomers.GroupBy(c => new { c.Customer }).ToList();
                var processedGroups = 0;
                //await _customerRepository.BeginTransactionAsync();

                try
                {
                    foreach (var group in customerGroups)
                    {
                        await ProcessCustomerGroupAsync(group.Key.Customer, group.ToList(), result);
                        processedGroups++;
                    }

                    //await _customerRepository.CommitTransactionAsync();
                    await _customerRepository.ClearGeoCacheAsync();
                    result.Success = true;
                    result.Message = BuildSuccessMessage(result);

                    _logger.LogInformation(result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during customer sync");
                    //await _customerRepository.RollbackTransactionAsync();
                    result.Success = false;
                    result.Message = $"Unexpected error during customer sync";
                    throw new CustomerSyncException($"Unexpected error during customer sync", ex);
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = sapEx.Message;
                _logger.LogError(sapEx.InnerException, result.Message);
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Unexpected error during customer sync";
                _logger.LogError(ex, result.Message);
                throw new CustomerSyncException(result.Message, ex);
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
                #region global customer processing (commented out)
                //var globalCustomerObj = await _mappingHelper.MapSapToXontGlobalCustomerAsync(
                //    sapCustomers[0]
                //);

                //var globalCustomerExisting = await _customerRepository.GetGlobalRetailerAsync(
                //    globalCustomerObj.RetailerCode
                //);

                //if (globalCustomerExisting == null)
                //{
                //    await _customerRepository.CreateGlobalRetailerAsync(globalCustomerObj);
                //    _logger.LogDebug(
                //        "Created global retailer: {RetailerCode}",
                //        globalCustomerObj.RetailerCode
                //    );
                //}
                //else if (
                //    _mappingHelper.HasGlobalRetailerChanges(
                //        globalCustomerExisting,
                //        globalCustomerObj
                //    )
                //)
                //{
                //    _mappingHelper.UpdateGlobalCustomer(globalCustomerExisting, globalCustomerObj);
                //    _logger.LogDebug(
                //        "Updated global retailer: {RetailerCode}",
                //        globalCustomerObj.RetailerCode
                //    );
                //}
                #endregion

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
                            await _customerRepository.AddOrUpdateRetailerDistributionChannelAsync(
                                xontRetailer.BusinessUnit,
                                xontRetailer.RetailerCode,
                                sapCustomer.Distributionchannel
                            );
                            result.NewCustomers++;
                        }
                        else
                        {
                            var (hasRetailerChanges, hasGeoChanges, hasDistChannelChanged) =
                                await _mappingHelper.HasRetailerChanges(
                                    existing,
                                    xontRetailer,
                                    sapCustomer.PostalCode,
                                    sapCustomer.Distributionchannel
                                );

                            if (hasRetailerChanges || hasGeoChanges || hasDistChannelChanged)
                            {
                                if (hasRetailerChanges)
                                    _mappingHelper.UpdateCustomer(existing, xontRetailer);

                                if (hasGeoChanges)
                                    await _customerRepository.AddOrUpdateRetailerGeographicDataAsync(
                                        xontRetailer.BusinessUnit,
                                        xontRetailer.RetailerCode,
                                        sapCustomer.PostalCode ?? ""
                                    );

                                if (hasDistChannelChanged)
                                    await _customerRepository.AddOrUpdateRetailerDistributionChannelAsync(
                                        xontRetailer.BusinessUnit,
                                        xontRetailer.RetailerCode,
                                        sapCustomer.Distributionchannel
                                    );
                                result.UpdatedCustomers++;
                            }
                            else
                            {
                                result.SkippedCustomers++;
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
                            "Unexpected error during customer sync : {CustomerCode} ",
                            sapCustomer.Customer
                        );
                        result.FailedRecords++;

                        throw new CustomerSyncException(
                            $"Unexpected error during customer sync : {sapCustomer.Customer}",
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
                    "Unexpected error during customer sync : {CustomerCode}",
                    customerCode
                );
                result.FailedRecords += sapCustomers.Count;

                throw new CustomerSyncException(
                    $"Unexpected error during customer sync : {customerCode}",
                    customerCode,
                    ex
                );
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
