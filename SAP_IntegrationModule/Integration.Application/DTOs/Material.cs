using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Integration.Application.DTOs
{
    public class SapMaterialResponseDto
    {
        public string SalesOrganization { get; set; } = string.Empty;
        public string Distributionchannel { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string MaterialGroup1 { get; set; } = string.Empty;
        public string MaterialGroup2 { get; set; } = string.Empty;
        public string MaterialGroup3 { get; set; } = string.Empty;
        public string MaterialGroup4 { get; set; } = string.Empty;
        public string MaterialGroup5 { get; set; } = string.Empty;
        public string SalesUnit { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal ConversionFactor { get; set; }
        public string BatchControlFlag { get; set; } = string.Empty;
        public string stockproductupdate { get; set; } = string.Empty;
        public string TodaysDate { get; set; } = string.Empty;
    }

    public class XontMaterialSyncRequestDto
    {
        public string Date { get; set; } = string.Empty; 
    }

    public class MaterialSyncResultDto
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int NewMaterials { get; set; }
        public int UpdatedMaterials { get; set; }
        public int SkippedMaterials { get; set; }
        public int FailedRecords { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SyncDate { get; set; }
    }

}