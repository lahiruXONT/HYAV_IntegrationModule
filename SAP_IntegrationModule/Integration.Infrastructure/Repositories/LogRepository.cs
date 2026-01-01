using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Infrastructure.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly GlobalDbContext _context;
        private readonly ILogger<LogRepository> _logger;

        public LogRepository(
            GlobalDbContext context,
            ILogger<LogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<long> LogRequestAsync(string businessUnit, string username,string methodName, string message, string messageType = "I")
        {
            try
            {
                var log = new RequestLog
                {
                    BusinessUnit = businessUnit,
                    UserName = username,
                    MethodName = methodName,
                    Message = message.Length > 1000 ?  message.Substring(0, 1000) : message,//Nazeer check this we may need to increase the size
                    MessageType = messageType,
                    UpdatedOn = DateTime.Now,
                };

                await _context.RequestLogs.AddAsync(log);
                await _context.SaveChangesAsync();

                return log.RecID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log request for {Username} in {BusinessUnit}", username, businessUnit);
                return 0;
            }
        }

        public async Task LogErrorAsync(string businessUnit, string username, string methodName, string error, long requestLogId, string errorType = "E")
        {
            try
            {
                var errorLog = new ErrorLog
                {
                    BusinessUnit = businessUnit,
                    UserName = username,
                    MethodName = methodName,
                    ErrorOn = DateTime.Now,
                    ErrorType = errorType,
                    Error = error,//Nazeer check this we may need to increase the size
                    RequestLogID = requestLogId,
                    UpdatedOn = DateTime.Now,
                };

                await _context.ErrorLogs.AddAsync(errorLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log error for RequestLogID: {RequestLogId}", requestLogId);
            }
        }
    }
}
