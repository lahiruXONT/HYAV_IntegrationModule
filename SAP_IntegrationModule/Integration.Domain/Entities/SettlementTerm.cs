using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class SettlementTerm
{
    public string BusinessUnit { get; set; } = string.Empty;
    public string SourceModuleCode { get; set; } = string.Empty;
    public string SettlementTermsCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SAPSettlementTermsCode { get; set; } = string.Empty;
}
