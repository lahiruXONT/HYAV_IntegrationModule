using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.Interfaces;


public interface ILogRepository
{
    Task<long> LogRequestAsync(string businessUnit, string username, string methodName, string message, string messageType = "I");
    Task LogErrorAsync(string businessUnit, string username, string methodName, string error, long requestLogId, string errorType );
}