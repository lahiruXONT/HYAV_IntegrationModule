using Integration.Domain.Entities;

namespace Integration.Application.DTOs;

//public sealed class StockPostingRequestDto
//{
//    // Header Information (1:1)
//    public DateTime PostingDate { get; set; }
//    public string? HeaderText { get; set; }

//    // Item Information (1:M)
//    public List<StockPostingItemDto>? Items { get; set; }
//}

//public sealed class StockPostingItemDto
//{
//    public string? Material { get; set; }
//    public string? ReceivingPlant { get; set; }
//    public string? ReceivingStorageLoc { get; set; }
//    public string? Batch { get; set; }
//    public string? Reference { get; set; } // Optional
//    public decimal Quantity { get; set; }
//}

//public sealed class StockPostingResponseDto
//{
//    public bool IsSuccess { get; set; } // Mapped from E_RESULT (1=Success)
//    public string? Reason { get; set; } // E_REASON
//    public string? MaterialDocumentNumber { get; set; } // MAT_DOC_NUM
//}

public sealed class StockOutSapRequestDto
{
    public string? MaterialDocumentNumber { get; set; } // MAT_DOC_NUM
    public DateTime SyncDate { get; set; }
}

public sealed class StockOutSapResponseDto
{
    public string? Division { get; set; }
    public string? Plant { get; set; }
    public string? StorageLocation { get; set; }
    public string? Material { get; set; }
    public decimal Quantity { get; set; }
    public string? ReceivingBatch { get; set; }
    public DateTime BatchExpiryDate { get; set; } // Expiry Date
    public DateTime PostingDate { get; set; }
    public DateTime EnteredOnAt { get; set; } // Last updated date and time
    public string? MaterialDocumentNumber { get; set; }
}

//public sealed class StockOutXontRequestDto
//{
//    public StockTransaction? StockTransactionEntry { get; set; }
//}
public sealed class StockOutXontResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string? MaterialDocumentNumber { get; set; }
}

public sealed class StockInXontRequestDto
{
    public string BusinessUnit { get; set; } = string.Empty;
    public string MaterialDocumentNumber { get; set; } = string.Empty; // MAT_DOC_NUM
}

public sealed class StockInXontResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public required StockTransaction StockDetails { get; set; }
    public string? MaterialDocumentNumber { get; set; }
}

public sealed class StockInSapRequestDto
{
    // HEADER (1:1)
    public DateTime POSTING_DATE { get; set; } // POSTING_DATE (DATS)
    public string HEADER_TXT { get; set; } = null!; // HEADER_TXT (CHAR25)

    // ITEMS (1:N)
    public List<StockInSapItemDto> I_ITEM { get; set; } = new();
}

public sealed class StockInSapItemDto
{
    public string Material { get; set; } = null!; // CHAR18
    public string ReceivingPlant { get; set; } = null!; // CHAR4
    public string ReceivingStorageLoc { get; set; } = null!; // CHAR4
    public string Batch { get; set; } = null!; // CHAR10
    public string? Reference { get; set; } // CHAR50 (O)
    public decimal Quantity { get; set; } // QUAN13,3
}

public sealed class StockInSapResponseDto
{
    public string E_RESULT { get; set; } // E_RESULT (1=Success)
    public string? E_REASON { get; set; } // E_REASON
    public string? MAT_DOC_NUM { get; set; } // MAT_DOC_NUM
}

public sealed class GetMaterialStockFromSapRequestDto
{
    public string Plant { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; } = DateTime.Now;
}

public sealed class GetMaterialStockFromSapResponseDto
{
    public string Material { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Plant { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
