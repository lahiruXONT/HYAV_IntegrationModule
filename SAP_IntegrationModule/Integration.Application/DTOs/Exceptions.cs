namespace Integration.Application.DTOs;

public sealed class IntegrationException : Exception
{
    public string RecordIdentifier { get; } = string.Empty;
    public string ErrorCode { get; }
    public List<string> Details { get; }

    public IntegrationException(string message, Exception innerException, string errorCode)
        : base(message, innerException)
    {
        RecordIdentifier = string.Empty;
        ErrorCode = errorCode;
        Details = new();
    }

    public IntegrationException(
        string message,
        string recordIdentifier,
        Exception innerException,
        string errorCode
    )
        : base(message, innerException)
    {
        RecordIdentifier = recordIdentifier;
        ErrorCode = errorCode;
        Details = new();
    }

    public IntegrationException(
        string message,
        string recordIdentifier,
        Exception innerException,
        string errorCode,
        List<string>? details = null
    )
        : base(message, innerException)
    {
        RecordIdentifier = recordIdentifier;
        ErrorCode = errorCode;
        Details = details ?? new();
    }
}

public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string SapError = "SAP_ERROR";
    public const string Database = "DATABASE_ERROR";
    public const string DatabaseUpdate = "DATABASE_UPDATE_ERROR";
    public const string Unexpected = "UNEXPECTED_ERROR";
    public const string UnAuthorize = "UNAUTHORIZED";
    public const string CustomerSync = "CUSTOMER_SYNC_ERROR";
    public const string MaterialSync = "MATERIAL_SYNC_ERROR";
    public const string BusinessUnitResolve = "BUSINESS_UNIT_RESOLVE_ERROR";
    public const string StockInSync = "STOCK_IN_SYNC_ERROR";
    public const string StockOutSync = "STOCK_IOUT_SYNC_ERROR";
    public const string MaterialStockSync = "MATERIAL_STOCK_SYNC_ERROR";
    public const string SalesSync = "SALES_SYNC_ERROR";
    public const string ReceiptSync = "RECEIPT_SYNC_ERROR";
    public const string InvoiceSync = "INVOICE_SYNC_ERROR";
}

public sealed class ValidationExceptionDto : Exception
{
    public ValidationExceptionDto(string message)
        : base(message) { }
}

public sealed class SapApiExceptionDto : Exception
{
    public SapApiExceptionDto(string message, Exception innerException)
        : base(message, innerException) { }
}

public sealed class BusinessUnitResolveException : Exception
{
    public BusinessUnitResolveException(string message, Exception innerException)
        : base(message, innerException) { }
}
