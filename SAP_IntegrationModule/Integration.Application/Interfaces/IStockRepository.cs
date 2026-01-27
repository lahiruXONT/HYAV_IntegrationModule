using Integration.Application.DTOs;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IStockRepository
{
    Task UpdateStockTransactionAsync(StockTransaction stock);
    Task<List<StockTransaction>> GetStockInTransactionDetails(
        string businessUnit,
        string materialDocumentNumber
    );
}
