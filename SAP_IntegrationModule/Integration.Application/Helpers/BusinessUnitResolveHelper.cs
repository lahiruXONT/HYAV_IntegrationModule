using Integration.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Helpers;

public sealed class BusinessUnitResolveHelper
{
    private readonly IBusinessUnitRepository _businessUnitRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BusinessUnitResolveHelper> _logger;

    public BusinessUnitResolveHelper(IBusinessUnitRepository businessUnitRepository, IConfiguration configuration, ILogger<BusinessUnitResolveHelper> logger)
    {
        _businessUnitRepository =
            businessUnitRepository
            ?? throw new ArgumentNullException(nameof(businessUnitRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ResolveBusinessUnitAsync(string salesOrg, string division)
    {

        if (string.IsNullOrWhiteSpace(division))
            throw new ArgumentException("Division cannot be null or empty", nameof(division));

        try
        {
            var businessUnit = await _businessUnitRepository.GetBusinessUnitByDivisionAsync(division);

            if (businessUnit == null || string.IsNullOrWhiteSpace(businessUnit.BusinessUnit))
            {
                var errorMessage = $"No active business unit found for  Sales Organization: '{salesOrg}' Division: '{division}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            return businessUnit.BusinessUnit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving business unit for Division: {Division}", division);
            throw;
        }
    }

    public async Task<BusinessUnitConfig> GetBusinessUnitConfigAsync(string businessUnitCode)
    {
        if (string.IsNullOrWhiteSpace(businessUnitCode))
            throw new ArgumentException("Business unit code cannot be null or empty", nameof(businessUnitCode));

        try
        {
            var businessUnit = await _businessUnitRepository.GetBusinessUnitByCodeAsync(businessUnitCode);

            if (businessUnit == null || string.IsNullOrWhiteSpace(businessUnit?.BusinessUnit))
            {
                var errorMessage = $"Business unit '{businessUnitCode}' not found ";
                _logger.LogError(errorMessage);
                throw new KeyNotFoundException(errorMessage);

            }
            var connectionString = BuildConnectionString(businessUnit?.DatabaseName ?? "");

            var config = new BusinessUnitConfig
            {
                BusinessUnitCode = businessUnit?.BusinessUnit ??"",
                DatabaseName = businessUnit?.DatabaseName ?? "",
                BusinessUnitName = businessUnit?.BusinessUnitName ?? "",
                Division = businessUnit?.Division ?? "",
                ConnectionString = connectionString,
            };
            return config;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business unit config for: {Code}", businessUnitCode);
            throw;
        }
    }


    public async Task<List<BusinessUnitConfig>> GetAllBusinessUnitsAsync()
    {

        try
        {
            var businessUnits = await _businessUnitRepository.GetAllActiveBusinessUnitsAsync();

            var configs = businessUnits.Select(bu => new BusinessUnitConfig
            {
                BusinessUnitCode = bu.BusinessUnit,
                DatabaseName = bu.DatabaseName,
                BusinessUnitName = bu.BusinessUnitName,
                Division = bu.Division,
                ConnectionString = BuildConnectionString(bu.DatabaseName),
            }).ToList();

            return configs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Sales Organization exists: {salesOrg}", salesOrg);
            throw;
        }
    }

    public async Task<bool> SalesOrgDivisionExistsAsync(string salesOrg,string division)
    {
        if (string.IsNullOrWhiteSpace(division))
            throw new ArgumentException("Division cannot be null or empty", nameof(division));
        if (string.IsNullOrWhiteSpace(salesOrg))
            throw new ArgumentException("Sales organization cannot be null or empty", nameof(salesOrg));

        try
        {
            return await _businessUnitRepository.BusinessUnitExistBySalesOrgDivisionAsync(salesOrg,division);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if division exists: {Division}", division);
            throw;
        }
    }

    public async Task<bool> BusinessUnitExistsAsync(string businessUnitCode)
    {
        if (string.IsNullOrWhiteSpace(businessUnitCode))
            return false;

        try
        {
            var unit = await _businessUnitRepository.GetBusinessUnitByCodeAsync(businessUnitCode);
            return unit != null && !string.IsNullOrWhiteSpace(unit.BusinessUnit) ;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if business unit exists: {Code}", businessUnitCode);
            throw;
        }
    }


    public string BuildConnectionString(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

        var baseConnectionString = _configuration.GetConnectionString("GlobalDatabase");
        if (string.IsNullOrEmpty(baseConnectionString))
        {
            throw new InvalidOperationException("GlobalDatabase connection string is not configured ");
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            };

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building connection string for database: {Database}", databaseName);
            throw;
        }
    }
}