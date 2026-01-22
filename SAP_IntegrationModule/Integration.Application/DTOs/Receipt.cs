using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs;

public class ReceiptRequestDto
{
    public required ReceiptHeader I_HEADER { get; set; }
    public List<ReceiptItem> I_ITEM { get; set; } = new List<ReceiptItem>();
}

public class SAPReceiptResponseDto
{
    public string E_RESULT { get; set; } = string.Empty;
    public string E_REASON { get; set; } = string.Empty;
    public string DOCUMENT_NUMBER { get; set; } = string.Empty;
}

public class ReceiptHeader
{
    public string COMP_CODE { get; set; } = string.Empty;
    public DateTime PSTNG_DATE { get; set; }
    public string CURRENCY_ISO { get; set; } = string.Empty;
    public string REF_DOC_NO { get; set; } = string.Empty;
}

public class ReceiptItem
{
    public string CUSTOMER { get; set; } = string.Empty;
    public string GL_ACCOUNT { get; set; } = string.Empty;
    public string PROFIT_CTR { get; set; } = string.Empty;
    public decimal AMOUNT { get; set; }
}

public sealed class XontReceiptSyncRequestDto
{
    public List<long> IDs { get; set; } = new List<long>();
}

public sealed class ReceiptSyncResultDto
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int SyncedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int FailedRecords { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public List<string>? Errors { get; set; }
}
