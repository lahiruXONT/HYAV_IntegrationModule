using Integration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Interfaces
{

    public interface IRetailerRepository : IDisposable
    {
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<Retailer?> GetByRetailerCodeAsync(string retailerCode, string businessUnit);
        Task<GlobalRetailer?> GetGlobalRetailerAsync(string retailerCode);
        Task CreateAsync(Retailer retailer);
        Task UpdateAsync(Retailer retailer);
        Task UpdateGlobalRetailerAsync(GlobalRetailer globalRetailer);
    }
}
