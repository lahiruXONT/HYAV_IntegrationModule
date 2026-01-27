using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class ReceivedSerialBatch
{
    public long RecID { get; set; }
    public required string BusinessUnit { get; set; }
    public required string TerritoryCode { get; set; }
    public required string WarehouseCode { get; set; }
    public required string LocationCode { get; set; }
    public required string ProductCode { get; set; }
    public string BatchSerialNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int TransactionCode { get; set; } = 0;
    public string TRNTypeRefIN { get; set; } = string.Empty;
    public int TRNTypeHeaderNumberIN { get; set; } = 0;
    public int TRNTypeDetailNumberIN { get; set; } = 0;
    public StockTransaction Transaction { get; set; } = null!;
}
