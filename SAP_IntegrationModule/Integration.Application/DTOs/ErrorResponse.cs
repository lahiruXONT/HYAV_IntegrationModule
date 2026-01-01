using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs
{

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

}
