using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Integration.Application.DTOs;
using Integration.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Helpers;

public sealed class MaterialMappingHelper
{
    private readonly BusinessUnitResolveHelper _businessUnitResolver;
    private readonly ILogger<MaterialMappingHelper> _logger;

    public MaterialMappingHelper(
        ILogger<MaterialMappingHelper> logger,
        BusinessUnitResolveHelper businessUnitResolver
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _businessUnitResolver =
            businessUnitResolver ?? throw new ArgumentNullException(nameof(businessUnitResolver));
    }

    public async Task<Product> MapSapToXontMaterialAsync(SapMaterialResponseDto sapMaterial)
    {
        await ValidateSapMaterialAsync(sapMaterial);

        try
        {
            var businessUnit = await _businessUnitResolver.ResolveBusinessUnitAsync(sapMaterial.SalesOrganization ??"", sapMaterial.Division ?? "");

            var product = new Product
            {
                // Mandatory fields
                ProductCode = sapMaterial.Material.Trim(),
                Description = sapMaterial.MaterialDescription?.Trim() ?? "",
                ProductGroup = GetMaterialGroup(sapMaterial.MaterialGroup1, ""),
                AlternateSearch = GetMaterialGroup(sapMaterial.MaterialGroup2, ""),
                StockCategory = GetMaterialGroup(sapMaterial.MaterialGroup3, ""),
                ProductTypeCode = GetMaterialGroup(sapMaterial.MaterialGroup4, ""),
                UOM1 = sapMaterial.SalesUnit.Trim(),
                UOM2 = sapMaterial.BaseUnit.Trim(),
                ConversionFactor = sapMaterial.ConversionFactor,
                BatchProcessingFlag = MapBatchFlag(sapMaterial.BatchControlFlag),
                NonStockItemFlag = MapNonStockFlag(sapMaterial.stockproductupdate),

                // Business mappings
                BusinessUnit = businessUnit,
                //SalesOrganization = sapMaterial.SalesOrganization?.Trim(),
                //DistributionChannel = sapMaterial.Distributionchannel?.Trim(),
                //Division = sapMaterial.Division?.Trim(),

                // Default values
                //SortSequence = 0,
                //PartAttribute1 = string.Empty,
                //PartAttribute2 = string.Empty,
                //Weight = 0,
                //SMMachineType = string.Empty,
                //SMPlatformSize = string.Empty,
                //SMCapacity = string.Empty,
                //SMOperatingEnvironment = string.Empty,
                //StampingPeriod = 0,
                //WarrantyPeriod = 0,
                //FinishedProduct = "1",
                Status = "1",
                SalableFlag = "1",
                BatchControlPrice = "0",
                TaxGroupCode = "VAT",
                TaxGroupValue = "V1",

                // Timestamps
                CreatedOn = DateTime.Now,
                UpdatedOn = ParseSapDate(sapMaterial.TodaysDate),
                CreatedBy = "SAP_SYNC",
                UpdatedBy = "SAP_SYNC",
            };

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to map SAP material {Code}. SalesOrg: {SalesOrg}, Division: {Division}",
                sapMaterial.Material,
                sapMaterial.SalesOrganization,
                sapMaterial.Division
            );
            throw;
        }
    }

    private async Task ValidateSapMaterialAsync(SapMaterialResponseDto sapMaterial)
    {
        if (sapMaterial == null)
            throw new ArgumentNullException(nameof(sapMaterial));

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sapMaterial.Material))
            errors.Add("Material code is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.MaterialDescription))
            errors.Add("Material description is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.SalesOrganization))
        {
            errors.Add("Sales organization is required");


        if (!string.IsNullOrWhiteSpace(sapMaterial.Division) && !await _businessUnitResolver.DivisionExistsAsync(sapMaterial.Division))
        {
            errors.Add($"Division '{sapMaterial.Division}' not found ");
        }
        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            throw new ValidationExceptionDto(errorMessage);
        }
    }

    public bool HasChanges(Product existing, Product updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        return existing.Description != updated.Description
            || existing.Description2 != updated.Description2
            || existing.ProductGroup != updated.ProductGroup
            || existing.AlternateSearch != updated.AlternateSearch
            || existing.StockCategory != updated.StockCategory
            || existing.ProductTypeCode != updated.ProductTypeCode
            || existing.UOM1 != updated.UOM1
            || existing.UOM2 != updated.UOM2
            || existing.ConversionFactor != updated.ConversionFactor
            || existing.BatchProcessingFlag != updated.BatchProcessingFlag
            || existing.NonStockItemFlag != updated.NonStockItemFlag
            ||
            //existing.SalesOrganization != updated.SalesOrganization ||
            //existing.DistributionChannel != updated.DistributionChannel ||
            //existing.Division != updated.Division
            existing.SMMachineType != updated.SMMachineType
            || existing.SMPlatformSize != updated.SMPlatformSize
            || existing.SMCapacity != updated.SMCapacity
            || existing.SMOperatingEnvironment != updated.SMOperatingEnvironment
            || existing.StampingPeriod != updated.StampingPeriod
            || existing.WarrantyPeriod != updated.WarrantyPeriod
            || existing.Status != updated.Status
            || existing.FinishedProduct != updated.FinishedProduct
            || existing.SalableFlag != updated.SalableFlag
            || existing.BatchControlPrice != updated.BatchControlPrice
            || existing.TaxGroupCode != updated.TaxGroupCode
            || existing.TaxGroupValue != updated.TaxGroupValue;
    }

    public void UpdateMaterial(Product existing, Product updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        existing.Description = updated.Description;
        existing.Description2 = updated.Description2;
        existing.ProductGroup = updated.ProductGroup;
        existing.AlternateSearch = updated.AlternateSearch;
        existing.StockCategory = updated.StockCategory;
        existing.ProductTypeCode = updated.ProductTypeCode;
        existing.UOM1 = updated.UOM1;
        existing.UOM2 = updated.UOM2;
        existing.ConversionFactor = updated.ConversionFactor;
        existing.BatchProcessingFlag = updated.BatchProcessingFlag;
        existing.NonStockItemFlag = updated.NonStockItemFlag;
        //existing.SalesOrganization = updated.SalesOrganization;
        //existing.DistributionChannel = updated.DistributionChannel;
        //existing.Division = updated.Division;
        existing.SMMachineType = updated.SMMachineType;
        existing.SMPlatformSize = updated.SMPlatformSize;
        existing.SMCapacity = updated.SMCapacity;
        existing.SMOperatingEnvironment = updated.SMOperatingEnvironment;
        existing.StampingPeriod = updated.StampingPeriod;
        existing.WarrantyPeriod = updated.WarrantyPeriod;
        existing.Status = updated.Status;
        existing.FinishedProduct = updated.FinishedProduct;
        existing.SalableFlag = updated.SalableFlag;
        existing.BatchControlPrice = updated.BatchControlPrice;
        existing.TaxGroupCode = updated.TaxGroupCode;
        existing.TaxGroupValue = updated.TaxGroupValue;

        existing.UpdatedOn = DateTime.Now;
        existing.UpdatedBy = "SAP_SYNC";
    }

    public bool HasValidMaterialCode(string materialCode)
    {
        return !string.IsNullOrWhiteSpace(materialCode) && materialCode.Length <= 40;
    }

    public async Task<bool> HasValidBusinessUnitAsync(string businessUnit)
    {
        if (string.IsNullOrWhiteSpace(businessUnit))
            return false;

        return await _businessUnitResolver.BusinessUnitExistsAsync(businessUnit);
    }


    private DateTime ParseSapDate(string sapDate)
    {
        if (string.IsNullOrEmpty(sapDate))
            return DateTime.Now;

        try
        {
            if (
                DateTime.TryParseExact(
                    sapDate,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
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
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result
                )
            )
            {
                return result;
            }
            return DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing SAP date: {Date}, using current date", sapDate);
            return DateTime.Now;
        }
    }

    private string GetMaterialGroup(string groupValue, string defaultValue)
    {
        return !string.IsNullOrEmpty(groupValue) && groupValue.Length <= 10
            ? groupValue.Trim()
            : defaultValue;
    }

    private string MapBatchFlag(string sapFlag)
    {
        return sapFlag?.Trim() == "1" ? "1" : "0";
    }

    private string MapNonStockFlag(string sapFlag)
    {
        return sapFlag?.Trim() == "1" ? "1" : "0";
    }
}
