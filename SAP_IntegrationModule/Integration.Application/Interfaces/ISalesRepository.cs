using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface ISalesRepository
{
    //Task BeginTransactionAsync();
    //Task CommitTransactionAsync();
    //Task RollbackTransactionAsync();
    Task<List<SalesOrderHeader>> GetXontSalesOrderAsync(DateTime currentDate);
    public Task<string?> GetDistributionChannelForCustomerAsync(
        string businessUnit,
        string customerCode
    );
    Task<SalesOrderHeader?> UpdateOrderStatusAsync(SalesOrderHeader order);

    //Task<SalesOrderSyncResultDto?> SyncXontSalesOrdersToSapAsync(SalesOrderHeader order);
    //Task<SalesInvoiceResponseDto?> GetSapSalesInvoiceAsync(SalesInvoiceRequestDto salesOrders);
    //Task<bool> UpdateSalesInvoiceInquiryAsync(SalesInvoiceResponseDto request);

    //Task<SalesOrderSyncResultDto?> GetXontSalesReturnAsync(DateTime fromDate, string orderComplete);
    //Task<SalesOrderSyncResultDto?> UpdateSalesReturnAsync(SalesOrderRequestDto salesOrders);
}
