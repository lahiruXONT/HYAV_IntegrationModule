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
        var businessUnit = await ValidateSapMaterialAndGetBusinessUnitAsync(sapMaterial);

        try
        {
            var product = new Product
            {
                // Mandatory fields
                ProductCode = sapMaterial.Material.Trim(),
                Description = ApplySapValueSafe(sapMaterial.MaterialDescription, ""),
                ProductGroup = ApplySapValueSafe(sapMaterial.MaterialGroup1, ""),
                AlternateSearch = ApplySapValueSafe(sapMaterial.MaterialGroup2, ""),
                StockCategory = ApplySapValueSafe(sapMaterial.MaterialGroup3, ""),
                ProductTypeCode = ApplySapValueSafe(sapMaterial.MaterialGroup4, ""),
                UOM1 = ApplySapValueSafe(sapMaterial.SalesUnit, ""),
                UOM2 = ApplySapValueSafe(sapMaterial.BaseUnit, ""),
                ConversionFactor = sapMaterial.ConversionFactor,
                BatchProcessingFlag = MapSapFlagSafe(sapMaterial.BatchControlFlag),
                NonStockItemFlag = MapSapFlagSafe(sapMaterial.stockproductupdate),

                BusinessUnit = businessUnit,

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
                FinishedProduct = "1",
                Status = "1",
                SalableFlag = "1",
                BatchControlPrice = "0",
                TaxGroupCode = "VAT",
                TaxGroupValue = "V1",
                Description2 = string.Empty,

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

    private async Task<string> ValidateSapMaterialAndGetBusinessUnitAsync(
        SapMaterialResponseDto sapMaterial
    )
    {
        if (sapMaterial == null)
            throw new ArgumentNullException(nameof(sapMaterial));

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sapMaterial.Material))
            errors.Add("Material code is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.MaterialDescription))
            errors.Add("Material description is required");

        var result = await _businessUnitResolver.TryResolveBusinessUnitAsync(
            sapMaterial.SalesOrganization,
            sapMaterial.Division
        );

        if (!result.IsValid)
        {
            errors.Add(result.Error);
            throw new ValidationExceptionDto(string.Join("; ", errors));
        }

        var businessUnit = result.BusinessUnit;

        if (string.IsNullOrWhiteSpace(sapMaterial.MaterialGroup1))
            errors.Add("MaterialGroup1 is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.MaterialGroup2))
            errors.Add("MaterialGroup2 is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.MaterialGroup3))
            errors.Add("MaterialGroup3 is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.MaterialGroup4))
            errors.Add("MaterialGroup4 is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.SalesUnit))
            errors.Add("SalesUnit is required");

        if (string.IsNullOrWhiteSpace(sapMaterial.BatchControlFlag))
            errors.Add("BatchControlFlag is required");

        if (errors.Any())
            throw new ValidationExceptionDto(string.Join("; ", errors));

        return businessUnit;
    }

    public bool HasMaterialChanges(Product existing, Product updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        return existing.Description != updated.Description
            || existing.ProductGroup != updated.ProductGroup
            || existing.AlternateSearch != updated.AlternateSearch
            || existing.StockCategory != updated.StockCategory
            || existing.ProductTypeCode != updated.ProductTypeCode
            || existing.UOM1 != updated.UOM1
            || existing.UOM2 != updated.UOM2
            || existing.ConversionFactor != updated.ConversionFactor
            || existing.BatchProcessingFlag != updated.BatchProcessingFlag
            || existing.NonStockItemFlag != updated.NonStockItemFlag;
    }

    public void UpdateMaterial(Product existing, Product updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        existing.Description = updated.Description;
        existing.ProductGroup = updated.ProductGroup;
        existing.AlternateSearch = updated.AlternateSearch;
        existing.StockCategory = updated.StockCategory;
        existing.ProductTypeCode = updated.ProductTypeCode;
        existing.UOM1 = updated.UOM1;
        existing.UOM2 = updated.UOM2;
        existing.ConversionFactor = updated.ConversionFactor;
        existing.BatchProcessingFlag = updated.BatchProcessingFlag;
        existing.NonStockItemFlag = updated.NonStockItemFlag;

        existing.UpdatedOn = DateTime.Now;
        existing.UpdatedBy = "SAP_SYNC";
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

    private string ApplySapValueSafe(string value, string defaultValue)
    {
        return !string.IsNullOrWhiteSpace(value) ? value.Trim() : defaultValue;
    }

    private string MapSapFlagSafe(string sapFlag)
    {
        return sapFlag?.Trim() == "1" ? "1" : "0";
    }

    #region Global customer mapping (commented out)
    //public async Task<GlobalProduct> MapSapToXontGlobalMaterialAsync(
    //    SapMaterialResponseDto sapMaterial
    //)
    //{
    //    await ValidateSapMaterialAsync(sapMaterial);

    //    try
    //    {
    //        var businessUnit = await _businessUnitResolver.ResolveBusinessUnitAsync(
    //            sapMaterial.SalesOrganization ?? "",
    //            sapMaterial.Division ?? ""
    //        );

    //        var product = new GlobalProduct
    //        {
    //            // Mandatory fields
    //            ProductCode = sapMaterial.Material.Trim(),
    //            Description = sapMaterial.MaterialDescription?.Trim() ?? "",
    //            ProductGroup = GetMaterialGroup(sapMaterial.MaterialGroup1, ""),
    //            AlternateSearch = GetMaterialGroup(sapMaterial.MaterialGroup2, ""),
    //            StockCategory = GetMaterialGroup(sapMaterial.MaterialGroup3, ""),
    //            ProductTypeCode = GetMaterialGroup(sapMaterial.MaterialGroup4, ""),
    //            UOM1 = sapMaterial.SalesUnit.Trim(),
    //            UOM2 = sapMaterial.BaseUnit.Trim(),
    //            ConversionFactor = sapMaterial.ConversionFactor,
    //            BatchProcessingFlag = MapBatchFlag(sapMaterial.BatchControlFlag),
    //            NonStockItemFlag = MapNonStockFlag(sapMaterial.stockproductupdate),

    //            // Default values
    //            //SortSequence = 0,
    //            //PartAttribute1 = string.Empty,
    //            //PartAttribute2 = string.Empty,
    //            //Weight = 0,
    //            //SMMachineType = string.Empty,
    //            //SMPlatformSize = string.Empty,
    //            //SMCapacity = string.Empty,
    //            //SMOperatingEnvironment = string.Empty,
    //            //StampingPeriod = 0,
    //            //WarrantyPeriod = 0,
    //            //FinishedProduct = "1",
    //            Status = "1",
    //            SalableFlag = "1",
    //            BatchControlPrice = "0",
    //            TaxGroupCode = "VAT",
    //            TaxGroupValue = "V1",
    //            Description2 = string.Empty,

    //            // Timestamps
    //            CreatedOn = DateTime.Now,
    //            UpdatedOn = ParseSapDate(sapMaterial.TodaysDate),
    //            CreatedBy = "SAP_SYNC",
    //            UpdatedBy = "SAP_SYNC",
    //        };

    //        return product;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(
    //            ex,
    //            "Failed to map SAP material {Code}. SalesOrg: {SalesOrg}, Division: {Division}",
    //            sapMaterial.Material,
    //            sapMaterial.SalesOrganization,
    //            sapMaterial.Division
    //        );
    //        throw;
    //    }
    //}

    //public bool HasGlobalMaterialChanges(GlobalProduct existing, GlobalProduct updated)
    //{
    //    if (existing == null)
    //        throw new ArgumentNullException(nameof(existing));
    //    if (updated == null)
    //        throw new ArgumentNullException(nameof(updated));

    //    return existing.Description != updated.Description
    //        || existing.ProductGroup != updated.ProductGroup
    //        || existing.AlternateSearch != updated.AlternateSearch
    //        || existing.StockCategory != updated.StockCategory
    //        || existing.ProductTypeCode != updated.ProductTypeCode
    //        || existing.UOM1 != updated.UOM1
    //        || existing.UOM2 != updated.UOM2
    //        || existing.ConversionFactor != updated.ConversionFactor
    //        || existing.BatchProcessingFlag != updated.BatchProcessingFlag
    //        || existing.NonStockItemFlag != updated.NonStockItemFlag;
    //}

    //public void UpdateGlobalMaterial(GlobalProduct existing, GlobalProduct updated)
    //{
    //    if (existing == null)
    //        throw new ArgumentNullException(nameof(existing));
    //    if (updated == null)
    //        throw new ArgumentNullException(nameof(updated));

    //    existing.Description = updated.Description;
    //    existing.ProductGroup = updated.ProductGroup;
    //    existing.AlternateSearch = updated.AlternateSearch;
    //    existing.StockCategory = updated.StockCategory;
    //    existing.ProductTypeCode = updated.ProductTypeCode;
    //    existing.UOM1 = updated.UOM1;
    //    existing.UOM2 = updated.UOM2;
    //    existing.ConversionFactor = updated.ConversionFactor;
    //    existing.BatchProcessingFlag = updated.BatchProcessingFlag;
    //    existing.NonStockItemFlag = updated.NonStockItemFlag;

    //    existing.UpdatedOn = DateTime.Now;
    //    existing.UpdatedBy = "SAP_SYNC";
    //}
    #endregion
}
