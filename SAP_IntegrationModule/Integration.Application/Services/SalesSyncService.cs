using System.Diagnostics;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public sealed class SalesSyncService : ISalesSyncService
{
    private readonly ISalesRepository _salesRepository;
    private readonly ISapClient _sapClient;

    private readonly SalesMappingHelper _mappingHelper;
    private readonly ILogger<SalesSyncService> _logger;

    public SalesSyncService(
        ISalesRepository salesRepository,
        ISapClient sapClient,
        SalesMappingHelper mappingHelper,
        ILogger<SalesSyncService> logger
    )
    {
        _salesRepository =
            salesRepository ?? throw new ArgumentNullException(nameof(salesRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    //3) Send Sales Orders:
    //-Take unprocessed orders
    //-Format according to SAP DTO
    //-Send to SAP
    //-Check response and update status

    //4) Get Sales Invoices:
    //-Request data
    //-Take Invoice Details
    //-Map with order details(need to clarify)
    //-Update Invoice Tables and Order Status
    //-Update Inquiry Table(New)

    //Send Sales Orders
    private void GetUprocessedOrders() { }

    private void MapOrderToDTO() { }

    private void TransferOrderToSAP() { }

    private void UpdateOrderStatus() { }

    //Get Sales Invoices
    private void GetSalesInvoicesFromSAP() { }

    private void MapDTOInvoiceToDomainObject() { }

    private void SaveInvoiceDetails() { }

    private void UpdateInvoiceStatus() { }

    public async Task<SalesOrderSyncResultDto> SyncSalesOrderToSapAsync(
        XontSalesSyncRequestDto request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new SalesOrderSyncResultDto { SyncDate = DateTime.UtcNow };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "SalesOrder",
                    ["RequestDate"] = request.Date.ToString().Trim(),
                }
            )
        )
        {
            try
            {
                _logger.LogInformation(
                    "Starting Sales Order sync for date: {Date}",
                    request.Date.ToString().Trim()
                );

                // 1. Pull unprocessed orders from DB
                var unprocessedOrders = await _salesRepository.GetXontSalesOrderAsync(request.Date);

                if (!unprocessedOrders.Any())
                {
                    result.Success = true;
                    result.Message = "No unprocessed sales orders found";
                    _logger.LogInformation(
                        "No unprocessed sales orders found for date: {Date}",
                        request.Date
                    );
                    return result;
                }

                _logger.LogInformation(
                    "Retrieved {Count} unprocessed sales orders",
                    unprocessedOrders.Count
                );

                // 2. Map and send each order to SAP
                foreach (var order in unprocessedOrders)
                {
                    try
                    {
                        var dto = await _mappingHelper.MapXontToSapSalesOrdersAsync(order);

                        var sapResponse = await _sapClient.SendSalesOrderAsync(dto); // <-- new method in SapApiClient

                        // 3. Update local order status based on SAP response
                        await _salesRepository.UpdateOrderStatusAsync(order);

                        result.TotalRecords++;
                        if (sapResponse.Result)
                            result.NewOrders++;
                        else
                            result.FailedOrders++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to sync order {OrderNo}", order.OrderNo);
                        result.FailedOrders++;
                    }
                }

                result.Success = result.FailedOrders == 0;
                result.Message = BuildSuccessMessage(result);

                _logger.LogInformation(
                    "Sales order sync completed in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Unexpected error: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during sales order sync");
                throw new IntegrationException(
                    $"Sales order sync failed: {ex.Message}",
                    ex,
                    ErrorCodes.SalesSync
                );
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    private string BuildSuccessMessage(SalesOrderSyncResultDto result)
    {
        var message = $"Sales Order sync completed. ";

        if (result.NewOrders > 0)
            message += $"New: {result.NewOrders}. ";

        if (result.UpdatedOrders > 0)
            message += $"Updated: {result.UpdatedOrders}. ";

        if (result.SkippedOrders > 0)
            message += $"Skipped: {result.SkippedOrders}. ";

        if (result.FailedOrders > 0)
            message += $"Failed: {result.FailedOrders}. ";

        message +=
            $"Total processed: {result.TotalRecords} records. Status: {result.Result} - {result.Reason}";

        return message;
    }
}
