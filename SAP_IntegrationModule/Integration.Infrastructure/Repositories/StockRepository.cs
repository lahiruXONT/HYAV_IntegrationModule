using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public Task<List<StockTransaction>> GetStockInTransactionDetails(
        string businessUnit,
        string materialDocumentNumber
    ) =>
        _context
            .StockTransactions.Include(h => h.ReceivedSerialBatches)
            .Where(s =>
                s.BusinessUnit == businessUnit
                && s.TransactionReference == materialDocumentNumber
                && s.MovementType == "1"
            )
            .ToListAsync();
}
