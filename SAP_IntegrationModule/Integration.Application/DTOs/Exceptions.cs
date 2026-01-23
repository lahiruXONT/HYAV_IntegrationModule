namespace Integration.Application.DTOs;

public abstract class IntegrationException : Exception
{
    public string ErrorCode { get; }
    public List<string> Details { get; }

    protected IntegrationException(string message, string errorCode, List<string>? details = null)
        : base(message)
    {
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

public sealed class CustomerSyncException : Exception
{
    public string CustomerCode { get; }

    public CustomerSyncException(string message, Exception innerException)
        : base(message, innerException)
    {
        CustomerCode = string.Empty;
    }

    public CustomerSyncException(string message, string customerCode, Exception innerException)
        : base(message, innerException)
    {
        CustomerCode = customerCode;
    }
}

public sealed class MaterialSyncException : Exception
{
    public string MaterialCode { get; }

    public MaterialSyncException(string message, Exception innerException)
        : base(message, innerException)
    {
        MaterialCode = string.Empty;
    }

    public MaterialSyncException(string message, string materialCode, Exception innerException)
        : base(message, innerException)
    {
        MaterialCode = materialCode;
    }
}

public sealed class SalesSyncException : Exception
{
    public string OrderNo { get; }

    public SalesSyncException(string message, Exception innerException)
        : base(message, innerException)
    {
        OrderNo = string.Empty;
    }

    public SalesSyncException(string message, string orderNo, Exception innerException)
        : base(message, innerException)
    {
        OrderNo = orderNo;
    }
}

public sealed class ReceiptSyncException : Exception
{
    public string DocumentNumberSystem { get; }

    public ReceiptSyncException(string message, Exception innerException)
        : base(message, innerException)
    {
        DocumentNumberSystem = string.Empty;
    }

    public ReceiptSyncException(string message, string receiptNo, Exception innerException)
        : base(message, innerException)
    {
        DocumentNumberSystem = receiptNo;
    }
}

public sealed class MaterialStockSyncException : Exception
{
    public string MaterialCode { get; }

    public MaterialStockSyncException(string message, Exception innerException)
        : base(message, innerException)
    {
        MaterialCode = string.Empty;
    }

    public MaterialStockSyncException(string message, string materialCode, Exception innerException)
        : base(message, innerException)
    {
        MaterialCode = materialCode;
    }
}

public sealed class BusinessUnitResolveException : Exception
{
    public BusinessUnitResolveException(string message, Exception innerException)
        : base(message, innerException) { }
}

public sealed class InvoiceSyncException : Exception
{
    public string OrderNo { get; }

    public InvoiceSyncException(string message, Exception innerException)
        : base(message, innerException)
    {
        OrderNo = string.Empty;
    }

    public InvoiceSyncException(string message, string orderNo, Exception innerException)
        : base(message, innerException)
    {
        OrderNo = orderNo;
    }
}
