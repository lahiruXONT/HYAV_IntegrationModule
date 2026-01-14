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
