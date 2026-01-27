using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Integration.Application.DTOs;

public sealed class SapInvoiceResponseDto
{
    public string E_RESULT { get; set; } = string.Empty;
    public string E_REASON { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string InvoiceDate { get; set; } = string.Empty;
    public string InvoiceStatus { get; set; } = string.Empty;
    public decimal TotalInvoiceValue { get; set; }
}

public sealed class XontInvoiceSyncRequestDto
{
    public string BusinessUnit { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public sealed class SAPInvoiceSyncRequestDto
{
    public string OrderNumber { get; set; } = string.Empty;
}

public sealed class InvoiceSyncResultDto
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int FailedRecords { get; set; }
    public int SyncedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; }
    public List<string> ValidationErrors { get; set; }

    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
