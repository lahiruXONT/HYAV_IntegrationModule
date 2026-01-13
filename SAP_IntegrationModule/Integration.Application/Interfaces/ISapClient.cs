using Integration.Application.DTOs;

namespace Integration.Application.Interfaces;

public interface ISapClient
{
    Task<List<SapCustomerResponseDto>> GetCustomerChangesAsync(XontCustomerSyncRequestDto request);
    Task<List<SapMaterialResponseDto>> GetMaterialChangesAsync(XontMaterialSyncRequestDto request);
    Task<SapSalesOrderResponseDTO> SendSalesOrderAsync(SalesOrderRequestDto request);
}
