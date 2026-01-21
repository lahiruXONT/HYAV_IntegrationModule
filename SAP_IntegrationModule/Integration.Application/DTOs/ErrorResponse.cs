using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs;

public sealed class ErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public List<string>? Details { get; set; }
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
}
