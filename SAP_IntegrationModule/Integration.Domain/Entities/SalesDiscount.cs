namespace Integration.Domain.Entities;

public abstract class SalesDiscountBase
{
    public long RecID { get; set; }

    //public long HeaderRecID { get; set; }
    public int Sequence { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string TerritoryCode { get; set; } = string.Empty;

    // Discount Logic
    public string DiscountReasonCode { get; set; } = string.Empty;
    public string DiscountCriteria { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal DiscountPercent { get; set; }
    public string DiscountedProductCode { get; set; } = string.Empty;

    // Trade Scheme Tracking
    public string TradeSchemeCode { get; set; } = string.Empty;
    public int TradeSchemeRecID { get; set; }
    public long DiscountPaymentRecID { get; set; }

    // Warehouse & Location
    public string WarehouseCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;

    // Prices & Costs
    public decimal Price { get; set; }
    public decimal StandardCostValue { get; set; }
    public decimal AverageCostValue { get; set; }
}

public sealed class SalesOrderDiscount : SalesDiscountBase
{
    public int OrderNo { get; set; }
    public short OrderLineNumber { get; set; } // Links to SalesOrderLine.OrderLineNo
    public string OrderComplete { get; set; } = "0";

    // Fields unique to Order stage
    public decimal AllocatedQtyU1 { get; set; }
    public decimal PickQuantityU1 { get; set; }
    public string SpecialDiscountBudgetExceeded { get; set; } = "0";

    // Reference to parent
    public SalesOrderHeader Header { get; set; } = null!;
}

public sealed class SalesInvoiceDiscount : SalesDiscountBase
{
    public int InvoiceNo { get; set; }
    public int InvoiceLineNumber { get; set; }
    public decimal DiscountPerUnit { get; set; }
    public int RebateRequestRecID { get; set; }

    // Reference to parent
    public SalesInvoiceHeader Header { get; set; } = null!;
}
