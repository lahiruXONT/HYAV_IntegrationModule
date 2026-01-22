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

    public async Task<StockInXontResponseDto> GetStockInTransactionDetails(
        StockInXontRequestDto stock
    )
    {
        var results = await _context
            .StockTransactions.Where(s =>
                s.BusinessUnit == stock.BusinessUnit
                && s.TransactionReference == stock.MaterialDocumentNumber
            )
            .FirstOrDefaultAsync();

        if (results == null)
            return null;

        return new StockInXontResponseDto
        {
            Success = true,
            StockDetails = results,
            MaterialDocumentNumber = results.TransactionReference,
            SyncDate = results.CreatedOn,
        };
    }
}
