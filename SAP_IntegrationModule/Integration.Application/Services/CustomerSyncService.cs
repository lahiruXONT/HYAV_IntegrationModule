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
        var result = new CustomerSyncResultDto { SyncDate = DateTime.Now };

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
                        $"Customer sync request validation failed: {string.Join("; ", validationErrors)}";
                    _logger.LogWarning(
                        "Customer sync validation failed: {ValidationErrors}",
                        string.Join("; ", validationErrors)
                    );
                    return result;
                }

                _logger.LogInformation("Starting customer sync for date: {Date}", request.Date);

                var sapCustomers = await _sapClient.GetCustomerChangesAsync(request);

                result.TotalRecords = sapCustomers?.Count ?? 0;

                if (sapCustomers == null || !sapCustomers.Any())
                {
                    result.Success = true;
                    result.Message = $"No customer changes found for date: {request.Date}";
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

                var customerGroups = sapCustomers.GroupBy(c => c.Customer).ToList();
                var processedGroups = 0;

                try
                {
                    foreach (var group in customerGroups)
                    {
                        await ProcessCustomerGroupAsync(
                            group.First().Customer,
                            group.ToList(),
                            result
                        );
                        processedGroups++;
                    }

                    await _customerRepository.ClearGeoCacheAsync();
                    result.Success = true;
                    result.Message = BuildSuccessMessage(result);

                    _logger.LogInformation(
                        "Customer sync completed successfully: {@Result}",
                        result
                    );
                }
                catch (Exception ex)
                    when (ex is not CustomerSyncException && ex is not SapApiExceptionDto)
                {
                    _logger.LogError(ex, "Unexpected error during customer sync processing");
                    result.Success = false;
                    result.Message =
                        $"Unexpected error during customer sync"
                        + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                        + (
                            !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                                ? $"; {ex.InnerException.Message}"
                                : ""
                        );
                    throw new CustomerSyncException(result.Message, ex);
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = sapEx.Message;
                _logger.LogError(
                    sapEx,
                    "SAP API error during customer sync: {Message}",
                    sapEx.Message
                );
                throw;
            }
            catch (Exception ex) when (ex is not CustomerSyncException)
            {
                result.Success = false;
                result.Message =
                    $"Unexpected error during customer sync"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );
                _logger.LogError(ex, "Unexpected error during customer sync");
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
                //    await _customerRepository.UpdateGlobalRetailerAsync(globalCustomerExisting);
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

                        await _customerRepository.ExecuteInTransactionAsync(async () =>
                        {
                            if (existing == null)
                            {
                                await _customerRepository.CreateRetailerAsync(xontRetailer);

                                await _customerRepository.AddOrUpdateRetailerGeographicDataAsync(
                                    xontRetailer.BusinessUnit,
                                    xontRetailer.RetailerCode,
                                    sapCustomer.PostalCode ?? string.Empty
                                );

                                await _customerRepository.AddOrUpdateRetailerDistributionChannelAsync(
                                    xontRetailer.BusinessUnit,
                                    xontRetailer.RetailerCode,
                                    sapCustomer.Distributionchannel ?? string.Empty
                                );

                                result.NewCustomers++;
                            }
                            else
                            {
                                var (hasRetailerChanges, hasGeoChanges, hasDistChannelChanged) =
                                    await _mappingHelper.HasRetailerChanges(
                                        existing,
                                        xontRetailer,
                                        sapCustomer.PostalCode ?? string.Empty,
                                        sapCustomer.Distributionchannel ?? string.Empty
                                    );

                                if (hasRetailerChanges)
                                {
                                    _mappingHelper.UpdateCustomer(existing, xontRetailer);
                                    await _customerRepository.UpdateRetailerAsync(existing);
                                }

                                if (hasGeoChanges)
                                {
                                    await _customerRepository.AddOrUpdateRetailerGeographicDataAsync(
                                        xontRetailer.BusinessUnit,
                                        xontRetailer.RetailerCode,
                                        sapCustomer.PostalCode ?? string.Empty
                                    );
                                }

                                if (hasDistChannelChanged)
                                {
                                    await _customerRepository.AddOrUpdateRetailerDistributionChannelAsync(
                                        xontRetailer.BusinessUnit,
                                        xontRetailer.RetailerCode,
                                        sapCustomer.Distributionchannel ?? string.Empty
                                    );
                                }

                                if (hasRetailerChanges || hasGeoChanges || hasDistChannelChanged)
                                    result.UpdatedCustomers++;
                                else
                                    result.SkippedCustomers++;
                            }
                        });
                    }
                    catch (ValidationExceptionDto valEx)
                    {
                        _logger.LogWarning(
                            "Validation failed for customer {CustomerCode}: {Message}",
                            sapCustomer.Customer,
                            valEx.Message
                        );
                        result.FailedRecords++;
                        result.ValidationErrors ??= new();
                        result.ValidationErrors.Add(
                            $"Customer {sapCustomer.Customer}: {valEx.Message}"
                        );
                    }
                    catch (BusinessUnitResolveException buEx)
                    {
                        _logger.LogWarning(
                            "Business unit resolution failed: {Message}",
                            buEx.Message
                        );
                        result.FailedRecords++;
                        result.ValidationErrors ??= new();
                        result.ValidationErrors.Add($"Business unit error: {buEx.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is not CustomerSyncException)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during customer processing for {CustomerCode}",
                    customerCode
                );
                result.FailedRecords += sapCustomers.Count;
                result.Message =
                    $"Unexpected error processing customer {customerCode}"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );
                throw new CustomerSyncException(result.Message, customerCode, ex);
            }
        }
    }

    private string BuildSuccessMessage(CustomerSyncResultDto result)
    {
        var message = $"Customer sync completed successfully. ";

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
