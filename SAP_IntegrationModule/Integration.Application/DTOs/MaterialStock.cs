using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs;

public sealed class XontMaterialStockSyncRequestDto
{
    public string Material { get; set; } = string.Empty;
}

public class SapMaterialStockSyncResponseDto
{
    public string E_RESULT { get; set; } = string.Empty;
    public string E_REASON { get; set; } = string.Empty;
    public string E_OUT { get; set; } = string.Empty;
    public List<SapMaterialStockSyncResponseItem> ITEM { get; set; } =
        new List<SapMaterialStockSyncResponseItem>();
}

public class SapMaterialStockSyncResponseItem
{
    public string MATERIAL { get; set; } = string.Empty;
    public decimal QUANTITY { get; set; }
}

public class MaterialStockSyncResultDto
{
    public bool Success { get; set; }
    public List<SapMaterialStockSyncResponseItem> ITEM { get; set; } =
        new List<SapMaterialStockSyncResponseItem>();
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
