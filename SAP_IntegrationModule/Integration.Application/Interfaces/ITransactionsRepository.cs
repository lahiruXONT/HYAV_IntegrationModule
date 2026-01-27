using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface ITransactionsRepository
{
    Task<List<Transaction>> GetUnsyncedReceiptsAsync(string BusinessUnit, List<long> recIds);
    Task UpdateTransactionAsync(Transaction transaction);
}
