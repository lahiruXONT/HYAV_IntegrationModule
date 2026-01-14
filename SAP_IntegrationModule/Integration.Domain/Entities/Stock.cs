namespace Integration.Domain.Entities;

public sealed class StockTransaction : BaseAuditableEntity
{
    public long RecId { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string TerritoryCode { get; set; } = string.Empty;
    public int TransactionCode { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime TransactionEnteredDate { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;

    // Movement quantities
    public decimal U1MovementQuantity { get; set; }
    public decimal Uom1PostTransactionStock { get; set; }
    public string Uom1 { get; set; } = string.Empty;
    public decimal U2MovementQuantity { get; set; }
    public decimal Uom2PostTransactionStock { get; set; }
    public string Uom2 { get; set; } = string.Empty;

    // Pricing and Costing
    public decimal ConversionRate { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal PostTransactionAverageCost { get; set; }
    public decimal PostTransactionAverageCostLocation { get; set; }
    public decimal BatchCost { get; set; }
    public decimal StandardCost { get; set; }

    // References
    public string MovementReference { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public string MovementReason { get; set; } = string.Empty;
    public string TrnType { get; set; } = string.Empty;

    public string Status { get; set; } = "1";
    public string PdaTransaction { get; set; } = "0";
}

public sealed class StockRecord : BaseAuditableEntity
{
    public long RecId { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string TerritoryCode { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;

    // Core Stock Quantities
    public decimal StockU1 { get; set; }
    public decimal AllocatedU1 { get; set; }
    public decimal SalesOnOrderU1 { get; set; }

    // Costing
    public decimal StandardCost { get; set; }
    public decimal AverageCost { get; set; }

    // Audit for the last transaction that touched this record
    public string TRNType { get; set; } = string.Empty;
    public int TRNTypeHeaderNumber { get; set; }
}

public sealed class TerritoryProduct : BaseAuditableEntity
{
    public int RecId { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string TerritoryCode { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;

    public decimal TotalStockU1 { get; set; }
    public decimal TotalAllocatedU1 { get; set; }
    public decimal AverageCost { get; set; }
}

public sealed class CurrentSerialBatch : BaseAuditableEntity
{
    public long RecId { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string BatchSerialNumber { get; set; } = string.Empty;

    public decimal ReceivedQuantity { get; set; }
    public decimal BalanceQuantity { get; set; } // The actual physical stock left in this batch
    public decimal AllocatedQuantity { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public decimal BatchCost { get; set; }
}
