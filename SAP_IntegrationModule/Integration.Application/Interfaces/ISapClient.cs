using Integration.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Interfaces
{
    public interface ISapClient
    {
        Task<List<SapCustomerResponseDto>> GetCustomerChangesAsync(XontCustomerSyncRequestDto request);
        Task<List<SapMaterialResponseDto>> GetMaterialChangesAsync(XontMaterialSyncRequestDto request);
    }
}
