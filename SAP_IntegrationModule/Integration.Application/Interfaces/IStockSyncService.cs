using Integration.Application.DTOs;

namespace Integration.Application.Interfaces;

public interface IStockSyncService
{
    Task<StockOutXontResponseDto> SyncStockOutFromSapAsync(StockOutSapRequestDto request);
    Task<StockInXontResponseDto> SyncStockInFromXontAsync(StockInXontRequestDto request);
}
