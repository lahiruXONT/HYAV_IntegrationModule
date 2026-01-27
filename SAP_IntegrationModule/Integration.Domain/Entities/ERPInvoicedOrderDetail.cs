using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class ERPInvoicedOrderDetail
{
    public long RecID { get; set; }
    public string BusinessUnit { get; set; }
    public string TerritoryCode { get; set; }
    public string ExecutiveCode { get; set; }
    public string CustomerCode { get; set; }
    public string OrderNo { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalGoodsValue { get; set; }
    public decimal TotalInvoiceValue { get; set; }

    /// <summary>
    /// Invoice status: O - Open, P - Partial processed, C - Completely processed
    /// </summary>
    public string Status { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public byte[] TimeStamp { get; set; } = Array.Empty<byte>();
}
