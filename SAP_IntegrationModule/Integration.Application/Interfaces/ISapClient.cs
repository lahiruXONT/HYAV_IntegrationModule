using Integration.Application.DTOs;

namespace Integration.Application.Interfaces;

public interface ISapClient
{
    Task<List<SapCustomerResponseDto>> GetCustomerChangesAsync(XontCustomerSyncRequestDto request);
    Task<List<SapMaterialResponseDto>> GetMaterialChangesAsync(XontMaterialSyncRequestDto request);
    Task<SapSalesOrderResponseDto> SendSalesOrderAsync(SalesOrderRequestDto request);
    Task<StockOutSapResponseDto> GetStockOutTransactionDetails(StockOutSapRequestDto dto);
    Task<List<GetMaterialStockFromSapResponseDto>> GetLocationStockDetails(
        GetMaterialStockFromSapRequestDto dto
    );
    Task<SAPReceiptResponseDto> SendReceiptAsync(ReceiptRequestDto request);
    Task<SapMaterialStockSyncResponseDto?> GetMaterialStockAsync(
        XontMaterialStockSyncRequestDto request
    );
    Task<SapInvoiceResponseDto> GetInvoiceDataAsync(SAPInvoiceSyncRequestDto request);
}
