using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;

namespace Integration.Infrastructure.Repositories;

public class TransactionsRepository : ITransactionsRepository
{
    private readonly UserDbContext _context;

    public TransactionsRepository(UserDbContext context)
    {
        _context = context;
    }

    public Task<List<Transaction>> GetUnsyncedReceiptsAsync(List<long> recIds) =>
        _context
            .Transactions.Where(r =>
                r.IntegratedStatus == "0"
                && r.TransactionCode == 151
                && (!recIds.Any() || recIds.Contains(r.RecID))
            )
            .ToListAsync();

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }
}
