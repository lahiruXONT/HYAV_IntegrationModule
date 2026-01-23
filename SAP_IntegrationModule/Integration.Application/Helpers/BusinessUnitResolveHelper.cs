using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
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

    public async Task<(
        bool IsValid,
        string BusinessUnit,
        string Error
    )> TryResolveBusinessUnitAsync(string salesOrg, string division)
    {
        if (string.IsNullOrWhiteSpace(salesOrg))
            return (false, string.Empty, "Sales organization is required");

        if (string.IsNullOrWhiteSpace(division))
            return (false, string.Empty, "Division is required");

        try
        {
            var businessUnit = await _businessUnitRepository.GetBusinessUnitBySalesOrgDivisionAsync(
                salesOrg,
                division
            );

            if (businessUnit == null || string.IsNullOrWhiteSpace(businessUnit.BusinessUnit))
            {
                return (
                    false,
                    string.Empty,
                    $"No business unit found for Sales Organization: '{salesOrg}' Division: '{division}'"
                );
            }

            return (true, businessUnit.BusinessUnit, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error resolving business unit for SalesOrg: {SalesOrg}, Division: {Division}",
                salesOrg,
                division
            );
            throw new BusinessUnitResolveException(
                $"Error resolving business unit for SalesOrg: {salesOrg}, Division: {division}"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    ),
                ex
            );
        }
    }

    public async Task<ZYBusinessUnit> GetBusinessUnitDataAsync(string businessUnit)
    {
        if (string.IsNullOrWhiteSpace(businessUnit))
            throw new ArgumentException(
                "BusinessUnit cannot be null or empty",
                nameof(businessUnit)
            );
        try
        {
            var businessUnitData = await _businessUnitRepository.GetBusinessUnitByCodeAsync(
                businessUnit
            );

            if (
                businessUnitData == null
                || string.IsNullOrWhiteSpace(businessUnitData.BusinessUnit)
            )
            {
                var errorMessage = $"No active business unit found as {businessUnit}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            return businessUnitData;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error resolving business unit data for {businessUnit}",
                businessUnit
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
}
