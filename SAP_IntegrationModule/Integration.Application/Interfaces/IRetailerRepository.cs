using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IRetailerRepository
{
    //Task BeginTransactionAsync();
    //Task CommitTransactionAsync();
    //Task RollbackTransactionAsync();
    Task<Retailer?> GetByRetailerCodeAsync(string retailerCode, string businessUnit);
    Task<GlobalRetailer?> GetGlobalRetailerAsync(string code);
    Task CreateRetailerAsync(Retailer retailer);
    Task CreateGlobalRetailerAsync(GlobalRetailer retailer);
    Task<TerritoryPostalCode?> GetTerritoryCodeAsync(string postalCode);
    Task<bool> PostalCodeTerritoryExistsAsync(string postalCode);

    Task AddOrUpdateRetailerGeographicDataAsync(
        string businessUnit,
        string retailerCode,
        string postalCode
    );
    Task<bool> PostalCodeExistsForTownAsync(string businessUnit, string postalCode);
    Task<bool> DistributionChannelExistsAsync(string businessUnit, string Distributionchannel);

    Task AddOrUpdateRetailerDistributionChannelAsync(
        string BusinessUnit,
        string RetailerCode,
        string Distributionchannel
    );
    Task<(bool hasGeoChanges, bool hasDistChannelChanges)> CheckClassificationChangesAsync(
        string businessUnit,
        string retailerCode,
        string postalCode,
        string distributionChannel
    );
    Task ClearGeoCacheAsync();
}
