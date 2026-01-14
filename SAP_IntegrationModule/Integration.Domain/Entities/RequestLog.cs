namespace Integration.Domain.Entities;

public sealed class RequestLog
{
    public long RecID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }
}

public sealed class ErrorLog
{
    public long RecID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public DateTime? ErrorOn { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public long RequestLogID { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
