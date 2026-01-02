using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Integration.Domain.Entities;

public class GlobalProduct : BaseAuditableEntity
{
    public long RecID { get; set; }

    [StringLength(24)]
    public string ProductCode { get; set; } = string.Empty;

    [StringLength(40)]
    public string Description { get; set; } = string.Empty;

    [StringLength(30)]
    public string Description2 { get; set; } = string.Empty;

    [StringLength(10)]
    public string ProductGroup { get; set; } = string.Empty;

    [StringLength(10)]
    public string AlternateSearch { get; set; } = string.Empty;

    [StringLength(10)]
    public string StockCategory { get; set; } = string.Empty;

    [StringLength(10)]
    public string ProductTypeCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string UOM1 { get; set; } = string.Empty;

    [StringLength(10)]
    public string UOM2 { get; set; } = string.Empty;

    [Column(TypeName = "decimal(30,25)")]
    public decimal ConversionFactor { get; set; }

    [Column(TypeName = "decimal(9,0)")]
    public decimal SortSequence { get; set; }

    [StringLength(3)]
    public string PartAttribute1 { get; set; } = string.Empty;

    [StringLength(3)]
    public string PartAttribute2 { get; set; } = string.Empty;

    [Column(TypeName = "decimal(13,4)")]
    public decimal Weight { get; set; }

    [StringLength(40)]
    public string SMMachineType { get; set; } = string.Empty;

    [StringLength(40)]
    public string SMPlatformSize { get; set; } = string.Empty;

    [StringLength(40)]
    public string SMCapacity { get; set; } = string.Empty;

    [StringLength(40)]
    public string SMOperatingEnvironment { get; set; } = string.Empty;

    public int StampingPeriod { get; set; }

    public int WarrantyPeriod { get; set; }

    [StringLength(1)]
    public string Status { get; set; } = string.Empty;

    [StringLength(1)]
    public string FinishedProduct { get; set; } = string.Empty;

    [StringLength(1)]
    public string NonStockItemFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string SalableFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string BatchProcessingFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string BatchControlPrice { get; set; } = string.Empty;

    [StringLength(10)]
    public string TaxGroupCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string TaxGroupValue { get; set; } = string.Empty;
}