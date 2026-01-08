using Integration.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Helpers;

public sealed class BusinessUnitResolveHelper
{
    private readonly IBusinessUnitRepository _businessUnitRepository;
    private readonly ILogger<BusinessUnitResolveHelper> _logger;

    public BusinessUnitResolveHelper(
        IBusinessUnitRepository businessUnitRepository,
        ILogger<BusinessUnitResolveHelper> logger
    )
    {
        _businessUnitRepository =
            businessUnitRepository
            ?? throw new ArgumentNullException(nameof(businessUnitRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ResolveBusinessUnitAsync(string salesOrg, string division)
    {
        if (string.IsNullOrWhiteSpace(salesOrg))
            throw new ArgumentException(
                "Sales Organization cannot be null or empty",
                nameof(salesOrg)
            );
        if (string.IsNullOrWhiteSpace(division))
            throw new ArgumentException("Division cannot be null or empty", nameof(division));

        try
        {
            var businessUnit = await _businessUnitRepository.GetBusinessUnitBySalesOrgDivisionAsync(
                salesOrg,
                division
            );

            if (businessUnit == null || string.IsNullOrWhiteSpace(businessUnit.BusinessUnit))
            {
                var errorMessage =
                    $"No active business unit found for  Sales Organization: '{salesOrg}' Division: '{division}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            return businessUnit.BusinessUnit;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error resolving business unit for  Sales Organization: '{salesOrg}' Division: {Division}",
                salesOrg,
                division
            );
            throw;
        }
    }

    public async Task<bool> SalesOrgDivisionExistsAsync(string salesOrg, string division)
    {
        if (string.IsNullOrWhiteSpace(division))
            throw new ArgumentException("Division cannot be null or empty", nameof(division));
        if (string.IsNullOrWhiteSpace(salesOrg))
            throw new ArgumentException(
                "Sales organization cannot be null or empty",
                nameof(salesOrg)
            );

        try
        {
            return await _businessUnitRepository.BusinessUnitExistBySalesOrgDivisionAsync(
                salesOrg,
                division
            );
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
            return unit != null && !string.IsNullOrWhiteSpace(unit.BusinessUnit);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if business unit exists: {Code}",
                businessUnitCode
            );
            throw;
        }
    }
}
