using System.Diagnostics;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public class InvoiceSyncService : IInvoiceSyncService
{
    private readonly IInvoiceRepository _eRPInvoiceRepository;
    private readonly ISapClient _sapClient;
    private readonly ILogger<InvoiceSyncService> _logger;
    private readonly InvoiceMappingHelper _mappingHelper;

    public InvoiceSyncService(
        IInvoiceRepository eRPInvoiceRepository,
        ISapClient sapClient,
        ILogger<InvoiceSyncService> logger,
        InvoiceMappingHelper mappingHelper
    )
    {
        _eRPInvoiceRepository =
            eRPInvoiceRepository ?? throw new ArgumentNullException(nameof(eRPInvoiceRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
    }

    public async Task<InvoiceSyncResultDto> SyncInvoiceFromSapAsync(
        XontInvoiceSyncRequestDto request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new InvoiceSyncResultDto { SyncDate = DateTime.Now };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Invoice",
                    ["RequestDate"] = result.SyncDate,
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
                        $"Invoice sync request validation failed: {string.Join("; ", validationErrors)}";
                    _logger.LogWarning(
                        "Invoice sync validation failed: {ValidationErrors}",
                        string.Join("; ", validationErrors)
                    );
                    return result;
                }
                _logger.LogInformation("Starting Invoice sync : {Date}", result.SyncDate);

                var orders = await _eRPInvoiceRepository.GetOrdersByDateRangeAsync(
                    request.BusinessUnit,
                    request.FromDate,
                    request.ToDate
                );

                result.TotalRecords = orders?.Count ?? 0;

                if (orders == null || !orders.Any())
                {
                    result.Success = true;
                    result.Message = "No orders found for invoice synchronization";
                    _logger.LogInformation("No orders found for invoice synchronization");
                    return result;
                }

                _logger.LogInformation(
                    "Found {Count} pending invoice to sync",
                    result.TotalRecords
                );

                try
                {
                    foreach (var order in orders)
                    {
                        await ProcessInvoiceAsync(order, result);
                    }

                    if (result.FailedRecords > 0)
                        result.Success = false;
                    else
                        result.Success = true;
                    result.Message = BuildSuccessMessage(result);

                    _logger.LogInformation(
                        "invoice sync completed successfully: {@Result}",
                        result
                    );
                }
                catch (Exception ex)
                    when (ex is not IntegrationException && ex is not SapApiExceptionDto)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error during invoice processing. Processed {Processed}/{Total}.",
                        result.SyncedRecords,
                        result.TotalRecords
                    );

                    result.Success = false;
                    result.Message =
                        $"Unexpected error during invoice sync; failed after processing {result.SyncedRecords}/{result.TotalRecords} "
                        + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                        + (
                            !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                                ? $"; {ex.InnerException.Message}"
                                : ""
                        );
                    throw new IntegrationException(result.Message, ex, ErrorCodes.InvoiceSync);
                }
            }
            catch (Exception ex)
                when (ex is not IntegrationException && ex is not SapApiExceptionDto)
            {
                result.Success = false;
                result.Message =
                    $"Unexpected error during invoice sync"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );
                _logger.LogError(ex, "Unexpected error during invoice sync");
                throw new IntegrationException(result.Message, ex, ErrorCodes.InvoiceSync);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    private List<string> ValidateRequest(XontInvoiceSyncRequestDto request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request cannot be null");
            return errors;
        }
        if (string.IsNullOrWhiteSpace(request.BusinessUnit))
            errors.Add("Business Unit is required");
        if (request.FromDate == null || request.FromDate == DateTime.MinValue)
            errors.Add("From Date is required");
        if (request.ToDate == null || request.ToDate == DateTime.MinValue)
            errors.Add("To Date is required");

        if (request.ToDate > request.FromDate)
            errors.Add("To Date cannot be greater than From date");

        return errors;
    }

    private async Task ProcessInvoiceAsync(SalesOrderHeader order, InvoiceSyncResultDto result)
    {
        var OrderNumber = $"{order.SalesCategoryCode}/{order.OrderNo}";

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["BusinessUnit"] = order.BusinessUnit,
                    ["OrderNo"] = OrderNumber,
                }
            )
        )
        {
            try
            {
                _logger.LogDebug("Processing Order: {orderNo}", OrderNumber);

                var existingERPInvoiceDetail =
                    await _eRPInvoiceRepository.GetERPInvoiceDataByOrderAsync(
                        OrderNumber,
                        order.BusinessUnit,
                        order.RetailerCode,
                        order.ExecutiveCode,
                        order.TerritoryCode
                    );

                if (existingERPInvoiceDetail?.Any(x => x.Status == "C") == true)
                {
                    _logger.LogInformation(
                        "Invoice already completely processed for order {orderNo}. Skipping SAP call.",
                        OrderNumber
                    );

                    result.SkippedRecords++;
                    return;
                }

                var request = new SAPInvoiceSyncRequestDto { OrderNumber = OrderNumber };

                var sapResult = await _sapClient.GetInvoiceDataAsync(request);

                if (sapResult.E_RESULT == "1")
                {
                    var xontERPInvoice = await _mappingHelper.MapToERPInvoicedOrderDetail(
                        sapResult,
                        order
                    );

                    if (existingERPInvoiceDetail != null && existingERPInvoiceDetail.Any())
                    {
                        if (xontERPInvoice.Status == "O")
                        {
                            _logger.LogInformation(
                                "No changes detected for invoice of order {orderNo}, skipping update",
                                OrderNumber
                            );
                            result.SkippedRecords++;
                            return;
                        }
                        else if (
                            existingERPInvoiceDetail.Count() == 1
                            && existingERPInvoiceDetail.First().Status == "O"
                        )
                        {
                            var rec = existingERPInvoiceDetail.First();
                            rec.TotalInvoiceValue = xontERPInvoice.TotalInvoiceValue;
                            rec.Status = xontERPInvoice.Status;
                            rec.InvoiceDate = xontERPInvoice.InvoiceDate;
                            rec.UpdatedBy = "SAPSYNC";
                            rec.UpdatedOn = DateTime.Now;
                            await _eRPInvoiceRepository.UpdateERPInvoicedOrderDetailAsync(rec);
                            result.SyncedRecords++;
                        }
                        else
                        {
                            var rec = existingERPInvoiceDetail.FirstOrDefault(a =>
                                a.InvoiceDate == xontERPInvoice.InvoiceDate
                            );

                            if (rec != null)
                            {
                                if (
                                    rec.TotalInvoiceValue == xontERPInvoice.TotalInvoiceValue
                                    && rec.Status == xontERPInvoice.Status
                                )
                                {
                                    result.SkippedRecords++;
                                    _logger.LogInformation(
                                        "No changes detected for invoice of order {orderNo}, skipping update",
                                        OrderNumber
                                    );
                                    return;
                                }

                                rec.TotalInvoiceValue = xontERPInvoice.TotalInvoiceValue;
                                rec.Status = xontERPInvoice.Status;
                                rec.UpdatedBy = "SAPSYNC";
                                rec.UpdatedOn = DateTime.Now;
                                await _eRPInvoiceRepository.UpdateERPInvoicedOrderDetailAsync(rec);
                                result.SyncedRecords++;
                            }
                            else
                            {
                                await _eRPInvoiceRepository.CreateERPInvoicedOrderDetailAsync(
                                    xontERPInvoice
                                );
                                result.SyncedRecords++;
                            }
                        }
                    }
                    else
                    {
                        await _eRPInvoiceRepository.CreateERPInvoicedOrderDetailAsync(
                            xontERPInvoice
                        );
                        result.SyncedRecords++;
                    }

                    _logger.LogInformation(
                        "Successfully processed invoice for order {orderNo} from SAP",
                        OrderNumber
                    );
                }
                else
                {
                    var errorMessage =
                        $"SAP Returned Error for order {OrderNumber}: {sapResult.E_REASON}";
                    _logger.LogError(errorMessage);
                    result.Errors ??= new List<string>();
                    result.Errors.Add(errorMessage);
                    result.FailedRecords++;
                }
            }
            catch (ValidationExceptionDto valEx)
            {
                _logger.LogWarning(
                    "Validation failed for order {orderNo}: {Message}",
                    OrderNumber,
                    valEx.Message
                );
                result.FailedRecords++;
                result.ValidationErrors ??= new List<string>();
                result.ValidationErrors.Add($"Order {OrderNumber}: {valEx.Message}");
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = sapEx.Message;
                _logger.LogError(
                    sapEx,
                    "SAP API error processing invoice for order {orderNo}: {Message}",
                    OrderNumber,
                    sapEx.Message
                );
                throw;
            }
            catch (Exception ex) when (ex is not IntegrationException)
            {
                _logger.LogError(ex, "Error processing invoice {orderNo}", OrderNumber);
                result.FailedRecords += 1;
                result.Message =
                    $"Error processing invoice  {OrderNumber}"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );
                throw new IntegrationException(
                    result.Message,
                    OrderNumber.ToString(),
                    ex,
                    ErrorCodes.InvoiceSync
                );
            }
        }
    }

    private string BuildSuccessMessage(InvoiceSyncResultDto result)
    {
        var message = "Order Invoice sync completed successfully. ";

        if (result.SyncedRecords > 0)
            message += $"Success: {result.SyncedRecords}. ";

        if (result.SkippedRecords > 0)
            message += $"Skipped: {result.SkippedRecords}. ";

        if (result.FailedRecords > 0)
            message += $"Failed: {result.FailedRecords}. ";

        message += $"Total processed: {result.TotalRecords} in {result.ElapsedMilliseconds}ms";

        return message;
    }
}
