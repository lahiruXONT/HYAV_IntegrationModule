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
    private IDbContextTransaction? _transaction;

    public TransactionsRepository(UserDbContext context)
    {
        _context = context;
    }

    //public async Task BeginTransactionAsync() =>
    //    _transaction = await _context.Database.BeginTransactionAsync();

    //public async Task CommitTransactionAsync()
    //{
    //    await _context.SaveChangesAsync();
    //    await _transaction!.CommitAsync();
    //    await _transaction.DisposeAsync();
    //    _transaction = null;
    //}

    //public async Task RollbackTransactionAsync()
    //{
    //    if (_transaction != null)
    //    {
    //        await _transaction.RollbackAsync();
    //        await _transaction.DisposeAsync();
    //        _transaction = null;
    //    }
    //    _context.ChangeTracker.Clear();
    //}

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
