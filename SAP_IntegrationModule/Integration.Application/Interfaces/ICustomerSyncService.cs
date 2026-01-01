using Integration.Application.DTOs;

namespace Integration.Application.Interfaces
{
    public interface ICustomerSyncService
    {
        Task<CustomerSyncResultDto> SyncCustomersFromSapAsync(XontCustomerSyncRequestDto request);
    }
}