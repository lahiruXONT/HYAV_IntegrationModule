using Integration.Application.DTOs;

namespace Integration.Application.Interfaces;

public interface ISalesSyncService
{
    Task<SalesOrderSyncResultDto> SyncSalesOrderAsync(SalesOrderRequestDto request);
    Task<SalesInvoiceResponseDto> SyncSalesInvoiceAsync(SalesInvoiceRequestDto request);
}
