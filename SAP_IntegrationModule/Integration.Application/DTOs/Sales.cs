namespace Integration.Application.DTOs;

public sealed class SalesOrderDto
{
    // Header Information (1:1)
    public string OrderType { get; set; } = string.Empty;
    public string SalesOrg { get; set; } = string.Empty;
    public string DistributionChannel { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string SalesOffice { get; set; } = string.Empty;
    public string SalesGroup { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public DateTime CustomerReferenceDate { get; set; }
    public string SoldToParty { get; set; } = string.Empty;
    public string YourReference { get; set; } = string.Empty;

    // Relationship: 1 Header to Many Items
    public List<SalesOrderItemDto> Items { get; set; } = new List<SalesOrderItemDto>();
}

public sealed class SalesOrderItemDto
{
    // Item Information (1:M)
    public string Material { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public decimal OrderQuantity { get; set; }
    public string PoItemNumber { get; set; } = string.Empty;
    public decimal NetPrice { get; set; }
    public string HighLevelItem { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public string ProfitCenter { get; set; } = string.Empty;
    public string MarketingExecutive { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
}

public sealed class SalesOrderSyncResultDto
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int NewOrder { get; set; }
    public int UpdatedOrder { get; set; }
    public int SkippedOrder { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
}

public sealed class SapSalesOrderResponseDTO
{
    public bool Result { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long OrderNo { get; set; }
}

public sealed class SalesInvoiceRequestDto
{
    public int OrderNo { get; set; }
}

public sealed class SalesInvoiceResponseDto
{
    public int OrderNo { get; set; }
    public DateTime InvoiceDate { get; set; }

    /// O - Open, P - Partial processed, C - Completely processed
    public string? Status { get; set; }

    public decimal TotalInvoiceValue { get; set; }
}
