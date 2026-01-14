using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Helpers;

public sealed class SalesMappingHelper
{
    private readonly BusinessUnitResolveHelper _businessUnitResolver;
    private readonly ILogger<CustomerMappingHelper> _logger;
    private readonly ISalesRepository _salesRepository;

    public SalesMappingHelper(
        ILogger<CustomerMappingHelper> logger,
        BusinessUnitResolveHelper businessUnitResolver,
        ISalesRepository salesRepository
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _businessUnitResolver =
            businessUnitResolver ?? throw new ArgumentNullException(nameof(businessUnitResolver));
        _salesRepository =
            salesRepository ?? throw new ArgumentNullException(nameof(salesRepository));
    }

    public async Task<SalesOrderRequestDto> MapXontToSapSalesOrdersAsync(SalesOrderHeader order)
    {
        try
        {
            var salesOrderRequestDto = new SalesOrderRequestDto
            {
                OrderType = "Test" ?? string.Empty,
                SalesOrg = order?.SalesOrganization?.Trim() ?? string.Empty,
                //DistributionChannel = order?.DistributionChannel?.Trim() ?? string.Empty,
                Division = order?.BusinessUnit?.Trim() ?? string.Empty,
                SalesOffice = order?.TerritoryCode?.Trim() ?? string.Empty,
                SalesGroup = order?.SalesCategoryCode?.Trim() ?? string.Empty,
                CustomerReference = order?.CustomerOrderReference?.Trim() ?? string.Empty,
                CustomerReferenceDate = ParseSapDate(order?.OrderDate.ToString().Trim()),
                SoldToParty = order?.RetailerCode?.Trim() ?? string.Empty,
                YourReference = order?.OrderNo.ToString()?.Trim() ?? string.Empty,
            };

            var salesOrderRequestItemsDto = new List<SalesOrderItemDto>();

            foreach (var line in order?.Lines ?? Enumerable.Empty<SalesOrderLine>())
            {
                var item = new SalesOrderItemDto
                {
                    Material = line?.ProductCode?.Trim() ?? string.Empty,
                    Plant = order?.Plant?.Trim() ?? string.Empty,
                    OrderQuantity = line?.U1MovementQuantity ?? 0m,
                    PoItemNumber = line?.ProductCode?.Trim() ?? string.Empty,
                    NetPrice = line?.Price ?? 0m,
                    HighLevelItem = line?.ProductCode?.Trim() ?? string.Empty,
                    StorageLocation = line?.LocationCode?.Trim() ?? string.Empty,
                    ProfitCenter = order?.ProfitCenter?.Trim() ?? string.Empty,
                    MarketingExecutive = order?.ExecutiveCode?.Trim() ?? string.Empty,
                    JobType = order?.JobType?.Trim() ?? string.Empty,
                    ReferenceNumber = order?.ERPCustomerOrderRef?.Trim() ?? string.Empty,
                };
                salesOrderRequestItemsDto.Add(item);
            }

            salesOrderRequestDto.Items = salesOrderRequestItemsDto;

            await ValidateSapDtoSalesOrderAsync(salesOrderRequestDto);
            return salesOrderRequestDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to map XONT order {Code}. SalesOrg: {SalesOrg}, Division: {Division}",
                order?.OrderNo,
                order?.SalesOrganization,
                order?.BusinessUnit
            );
            throw;
        }
    }

    private async Task ValidateSapDtoSalesOrderAsync(SalesOrderRequestDto sapOrder)
    {
        if (sapOrder == null)
            throw new ValidationExceptionDto("SAP order data cannot be null");

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sapOrder.YourReference))
            errors.Add("OrderNo is required");

        if (sapOrder.Items?.Count <= 0)
            errors.Add($"Items missing in order: {sapOrder.YourReference}");

        if (string.IsNullOrWhiteSpace(sapOrder.SoldToParty))
            errors.Add("Customer is required");

        if (string.IsNullOrWhiteSpace(sapOrder.SalesOrg))
            errors.Add("Sales organization is required");

        if (string.IsNullOrWhiteSpace(sapOrder.Division))
            errors.Add("Division is required");

        if (
            !string.IsNullOrWhiteSpace(sapOrder.Division)
            && !string.IsNullOrWhiteSpace(sapOrder.SalesOrg)
        )
        {
            var exists = await _businessUnitResolver.SalesOrgDivisionExistsAsync(
                sapOrder.SalesOrg,
                sapOrder.Division
            );

            if (!exists)
            {
                errors.Add(
                    $"Business unit not found for SalesOrg: '{sapOrder.SalesOrg}' Division: '{sapOrder.Division}'"
                );
            }
        }

        if (string.IsNullOrWhiteSpace(sapOrder.SalesOffice))
        {
            errors.Add("SalesOffice is required");
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            throw new ValidationExceptionDto(errorMessage);
        }
    }

    private DateTime ParseSapDate(string? sapDate)
    {
        if (string.IsNullOrEmpty(sapDate))
            return DateTime.Now;

        try
        {
            if (
                DateTime.TryParseExact(
                    sapDate,
                    "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var result
                )
            )
            {
                return result;
            }

            if (
                DateTime.TryParseExact(
                    sapDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out result
                )
            )
            {
                return result;
            }

            _logger.LogWarning("Failed to parse SAP date: {Date}, using current date", sapDate);
            return DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing SAP date: {Date}, using current date", sapDate);
            return DateTime.Now;
        }
    }
}
