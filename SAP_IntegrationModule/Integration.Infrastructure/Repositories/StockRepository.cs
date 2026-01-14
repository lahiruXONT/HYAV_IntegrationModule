using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;

namespace Integration.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly UserDbContext _context;

    public StockRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task UpdateStockTransactionAsync(StockTransaction stock)
    {
        _context.StockTransactions.Add(stock);
        await _context.SaveChangesAsync();
    }
}
