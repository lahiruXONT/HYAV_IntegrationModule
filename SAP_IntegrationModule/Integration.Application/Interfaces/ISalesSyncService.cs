using Integration.Application.DTOs;

namespace Integration.Application.Interfaces;

public interface ISalesSyncService
{
    //Task<SalesInvoiceResponseDto> SyncSalesInvoiceAsync(SalesInvoiceRequestDto request);
    Task<SalesOrderSyncResultDto> SyncSalesOrderToSapAsync(XontSalesSyncRequestDto request);
}
