namespace Integration.Domain.Entities;

public abstract class SalesLineBase : BaseAuditableEntity
{
    public long RecID { get; set; }

    //public long HeaderRecID { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public decimal U1MovementQuantity { get; set; }
    public decimal Price { get; set; }
    public decimal GoodsValue { get; set; }
    public decimal VATValue { get; set; }
    public decimal LineDiscountValue { get; set; }

    // Costing & Margins
    public decimal StandardCostValue { get; set; }
    public decimal AverageCostValue { get; set; }
    public decimal StandardMarginValue { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
}

public sealed class SalesOrderLine : SalesLineBase
{
    public int OrderNo { get; set; }
    public short OrderLineNo { get; set; }
    public decimal AllocatedQtyU1 { get; set; }
    public decimal PickQuantityU1 { get; set; }

    // Reference to parent
    public SalesOrderHeader Header { get; set; } = null!;
}

public sealed class SalesInvoiceLine : SalesLineBase
{
    public int InvoiceNo { get; set; }
    public short InvoiceLineNo { get; set; }
    public int OrderNumber { get; set; } // Reference to source order

    // Reference to parent
    public SalesInvoiceHeader Header { get; set; } = null!;
}
