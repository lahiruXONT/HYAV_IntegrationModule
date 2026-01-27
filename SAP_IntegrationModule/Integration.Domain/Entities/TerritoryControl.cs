using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class TerritoryControl
{
    public long RecID { get; set; }
    public string TerritoryCode { get; set; } = string.Empty;
    public string BusinessUnit { get; set; } = string.Empty;
    public string PriceGroup { get; set; } = string.Empty;
    public string TradeSchemeGroup { get; set; } = string.Empty;
    public string Status { get; set; }
}
