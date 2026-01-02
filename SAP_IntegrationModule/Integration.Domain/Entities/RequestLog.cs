using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class RequestLog
{
    public long RecID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }
}

public class ErrorLog
{
    public long RecID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public DateTime? ErrorOn { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public long RequestLogID { get; set; }
    public DateTime UpdatedOn { get; set; }
}
