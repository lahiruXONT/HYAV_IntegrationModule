using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IRetailerRepository
{
    Task<Retailer?> GetByRetailerCodeAsync(string retailerCode, string businessUnit);
    Task<SettlementTerm?> GetSettlementTermAsync(string businessUnit, string PaymentTerm);
    Task<bool> SettlementTermExistsAsync(string businessUnit, string PaymentTerm);

    Task<bool> PostalCodeExistsForTownAsync(string businessUnit, string postalCode);
    Task<bool> DistributionChannelExistsAsync(string businessUnit, string Distributionchannel);

    Task<(bool hasGeoChanges, bool hasDistChannelChanges)> CheckClassificationChangesAsync(
        string businessUnit,
        string retailerCode,
        string postalCode,
        string distributionChannel
    );
    Task CreateRetailerAsync(Retailer retailer);
    Task UpdateRetailerAsync(Retailer retailer);
    Task AddOrUpdateRetailerGeographicDataAsync(
        string businessUnit,
        string retailerCode,
        string postalCode
    );
    Task AddOrUpdateRetailerDistributionChannelAsync(
        string BusinessUnit,
        string RetailerCode,
        string Distributionchannel
    );
    Task ClearGeoCacheAsync();
    Task ExecuteInTransactionAsync(Func<Task> value);
    #region Global Retailer Methods (commented out for now)
    //Task<GlobalRetailer?> GetGlobalRetailerAsync(string code);
    //Task CreateGlobalRetailerAsync(GlobalRetailer retailer);
    //Task UpdateGlobalRetailerAsync(GlobalRetailer retailer);
    #endregion
}
