using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities
{
        public class SyncErrorLog : BaseAuditableEntity
        {
            public long RecId { get; set; }
            public string SyncType { get; set; } = string.Empty; // "CUSTOMER", "MATERIAL", etc.
            public string BusinessUnit { get; set; } = string.Empty;
            public string RecordIdentifier { get; set; } = string.Empty; // CustomerCode, MaterialCode
            public string ErrorType { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
            public string StackTrace { get; set; } = string.Empty;
            public string SapData { get; set; } = string.Empty; // JSON of SAP data
            public string Status { get; set; } = "NEW"; // NEW, RETRY, RESOLVED
            public int RetryCount { get; set; }
            public DateTime? ResolvedOn { get; set; }
        }
    }