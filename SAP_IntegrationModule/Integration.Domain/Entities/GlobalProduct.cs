using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities
{
        public class GlobalProduct : BaseAuditableEntity
        {
            public long RecId { get; set; }
            public string ProductCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Description2 { get; set; } = string.Empty;
            public string ProductGroup { get; set; } = string.Empty;
            public string AlternateSearch { get; set; } = string.Empty;
            public string StockCategory { get; set; } = string.Empty;
            public string ProductTypeCode { get; set; } = string.Empty;
            public string UOM1 { get; set; } = string.Empty;
            public string UOM2 { get; set; } = string.Empty;
            public decimal ConversionFactor { get; set; }
            public decimal SortSequence { get; set; }
            public string PartAttribute1 { get; set; } = string.Empty;
            public string PartAttribute2 { get; set; } = string.Empty;
            public decimal Weight { get; set; }
            public string SMMachineType { get; set; } = string.Empty;
            public string SMPlatformSize { get; set; } = string.Empty;
            public string SMCapacity { get; set; } = string.Empty;
            public string SMOperatingEnvironment { get; set; } = string.Empty;
            public int StampingPeriod { get; set; }
            public int WarrantyPeriod { get; set; }
            public string Status { get; set; } = string.Empty;
            public string FinishedProduct { get; set; } = string.Empty;
            public string NonStockItemFlag { get; set; } = string.Empty;
            public string SalableFlag { get; set; } = string.Empty;
            public string BatchProcessingFlag { get; set; } = string.Empty;
            public string BatchControlPrice { get; set; } = string.Empty;
            public string TaxGroupCode { get; set; } = string.Empty;
            public string TaxGroupValue { get; set; } = string.Empty;
        }
    }
