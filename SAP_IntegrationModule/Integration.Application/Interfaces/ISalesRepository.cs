using Integration.Application.DTOs;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface ISalesRepository
{
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<SalesOrderHeader?> GetSfaSalesOrderAsync(DateTime fromDate, string orderComplete);
    Task<SalesOrderHeader?> SyncSfaSalesOrdersToSapAsync(SalesOrderHeader order);
    Task<SalesInvoiceResponseDto?> GetSapSalesInvoiceAsync(SalesInvoiceRequestDto salesOrders);
    Task<bool> UpdateSalesInvoiceInquiryAsync(SalesInvoiceResponseDto request);

    //Task<SalesOrderSyncResultDto?> GetSfaSalesReturnAsync(DateTime fromDate, string orderComplete);
    //Task<SalesOrderSyncResultDto?> UpdateSalesReturnAsync(SalesOrderRequestDto salesOrders);
}
