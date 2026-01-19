using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class Transaction
{
    public long RecID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string TerritoryCode { get; set; } = string.Empty;
    public int TransactionCode { get; set; } //only 151
    public string CustomerCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    //need to decide the reference from these
    public int DocumentNumberSystem { get; set; }
    public int SourceDocumentNumber { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;

    //end of need to decide

    //need to decide the post date from these or create date?
    public DateTime PostedDate { get; set; }
    public DateTime PostedOn { get; set; }

    //end of need to decide

    // SAP Integration Tracking
    public DateTime? IntegratedOn { get; set; }
    public string IntegratedStatus { get; set; } = "0";
    public string SAPDocumentNumber { get; set; } = string.Empty;
}
