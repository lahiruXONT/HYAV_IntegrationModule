using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Helpers
{
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> _correlationId = new();

        public static string CorrelationId
        {
            get => _correlationId.Value ?? "UNKNOWN";
            set => _correlationId.Value = value;
        }
    }
}
