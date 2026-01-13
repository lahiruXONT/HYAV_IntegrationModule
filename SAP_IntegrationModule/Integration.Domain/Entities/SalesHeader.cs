namespace Integration.Domain.Entities;

public abstract class SalesHeaderBase : BaseAuditableEntity
{
    public long RecID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string TerritoryCode { get; set; } = string.Empty;
    public string SalesCategoryCode { get; set; } = string.Empty;
    public string ExecutiveCode { get; set; } = string.Empty;
    public string RetailerCode { get; set; } = string.Empty;
    public string CustomerOrderReference { get; set; } = string.Empty;

    // Totals & Financials
    public decimal TotalGoodsValue { get; set; }
    public decimal TotalVATValue { get; set; }
    public decimal TotalInvoiceValue { get; set; }
    public string CurrencyCode { get; set; } = "LKR";
    public decimal ExchangeRate { get; set; }

    // SAP / Org Fields
    public string SalesOrganization { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string Status { get; set; } = "1";
    public string ERPCustomerOrderRef { get; set; } = string.Empty;
    public string ProfitCenter { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
}

public sealed class SalesOrderHeader : SalesHeaderBase
{
    public int OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? PromisedDate { get; set; }
    public string OrderComplete { get; set; } = "0";

    // SAP Integration Tracking
    public DateTime? IntegratedOn { get; set; }
    public string IntegratedStatus { get; set; } = "0";

    public IReadOnlyCollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    public IReadOnlyCollection<SalesOrderDiscount> Discounts { get; set; } =
        new List<SalesOrderDiscount>();
}

public sealed class SalesInvoiceHeader : SalesHeaderBase
{
    public int InvoiceNo { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int OrderNo { get; set; }
    public string DeliveryStatus { get; set; } = "0";

    public IReadOnlyCollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
    public IReadOnlyCollection<SalesInvoiceDiscount> Discounts { get; set; } =
        new List<SalesInvoiceDiscount>();
}
