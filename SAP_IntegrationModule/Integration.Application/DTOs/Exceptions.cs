using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs;

public abstract class IntegrationException : Exception
{
    public string ErrorCode { get; }
    public List<string> Details { get; }

    protected IntegrationException(
        string message,
        string errorCode,
        List<string>? details = null
    ) : base(message)
    {
        ErrorCode = errorCode;
        Details = details ?? new();
    }
}
public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string CustomerValidation = "CUSTOMER_VALIDATION_ERROR";
    public const string SapBusiness = "SAP_BUSINESS_ERROR";
    public const string SapTechnical = "SAP_TECHNICAL_ERROR";
    public const string Database = "DATABASE_ERROR";
    public const string Unexpected = "UNEXPECTED_ERROR";
}



public class ValidationExceptionDto : Exception
{
    public ValidationExceptionDto(string message) : base(message) { }
}


public class SapApiExceptionDto : Exception
{
    public SapApiExceptionDto(string message, Exception innerException) : base(message, innerException)
    {
    }
}
public class CustomerSyncException : Exception
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

public class MaterialSyncException : Exception
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
