using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IStockRepository
{
    Task UpdateStockTransactionAsync(StockTransaction stock);
}
