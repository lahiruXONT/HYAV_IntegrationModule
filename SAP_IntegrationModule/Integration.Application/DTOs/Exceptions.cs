using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs
{

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
}
