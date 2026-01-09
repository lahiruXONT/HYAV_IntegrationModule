using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Integration.Application.DTOs;

public sealed class SapMaterialResponseDto
{
    [StringLength(4)]
    public string SalesOrganization { get; set; } = string.Empty;

    [StringLength(2)]
    public string Distributionchannel { get; set; } = string.Empty;

    [StringLength(4)]
    public string Division { get; set; } = string.Empty;

    [StringLength(40)]
    public string Material { get; set; } = string.Empty;

    [StringLength(40)]
    public string MaterialDescription { get; set; } = string.Empty;

    [StringLength(3)]
    public string MaterialGroup1 { get; set; } = string.Empty;

    [StringLength(3)]
    public string MaterialGroup2 { get; set; } = string.Empty;

    [StringLength(3)]
    public string MaterialGroup3 { get; set; } = string.Empty;

    [StringLength(3)]
    public string MaterialGroup4 { get; set; } = string.Empty;

    [StringLength(3)]
    public string MaterialGroup5 { get; set; } = string.Empty;

    [StringLength(3)]
    public string SalesUnit { get; set; } = string.Empty;

    [StringLength(3)]
    public string BaseUnit { get; set; } = string.Empty;

    public decimal ConversionFactor { get; set; }

    [StringLength(1)]
    public string BatchControlFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string stockproductupdate { get; set; } = string.Empty;

    [StringLength(8)]
    public string TodaysDate { get; set; } = string.Empty;
}

public sealed class XontMaterialSyncRequestDto
{
    [Required(ErrorMessage = "Date is required")]
    [StringLength(8)]
    public string Date { get; set; } = string.Empty;
}

public sealed class MaterialSyncResultDto
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int NewMaterials { get; set; }
    public int UpdatedMaterials { get; set; }
    public int SkippedMaterials { get; set; }
    public int FailedRecords { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; internal set; }
    public List<string> ValidationErrors { get; internal set; }
}
