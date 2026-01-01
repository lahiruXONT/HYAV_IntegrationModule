using Integration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Interfaces
{
    public interface IBusinessUnitRepository : IDisposable
    {
        Task<List<BusinessUnitDBMAP>> GetAllActiveBusinessUnitsAsync();
        Task<BusinessUnitDBMAP> GetBusinessUnitByCodeAsync(string businessUnitCode);
        Task<BusinessUnitDBMAP> GetBusinessUnitByDivisionAsync( string division);
    }
}
