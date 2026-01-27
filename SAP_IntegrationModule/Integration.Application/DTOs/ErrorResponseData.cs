using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs;

public sealed class ErrorResponseData
{
    public DateTime Timestamp { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public string? CorrelationId { get; set; }
    public List<string>? Details { get; set; }
}
