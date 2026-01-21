using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Integration.Infrastructure.Repositories;

public class LogRepository : ILogRepository
{
    private readonly UserDbContext _context;

    public LogRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<long> LogRequestAsync(
        string businessUnit,
        string username,
        string methodName,
        string message,
        string messageType = "I"
    )
    {
        var log = new RequestLog
        {
            BusinessUnit = businessUnit,
            UserName = username,
            MethodName = methodName,
            Message = message.Length > 1000 ? message.Substring(0, 1000) : message, //Nazeer check this we may need to increase the size
            MessageType = messageType,
            UpdatedOn = DateTime.Now,
        };

        await _context.RequestLogs.AddAsync(log);
        await _context.SaveChangesAsync();

        return log.RecID;
    }

    public async Task LogErrorAsync(
        string businessUnit,
        string username,
        string methodName,
        string error,
        long requestLogId,
        string errorType = "E"
    )
    {
        var errorLog = new ErrorLog
        {
            BusinessUnit = businessUnit,
            UserName = username,
            MethodName = methodName,
            ErrorOn = DateTime.Now,
            ErrorType = errorType,
            Error = error, //Nazeer check this we may need to increase the size
            RequestLogID = requestLogId,
            CreatedOn = DateTime.Now,
            CreatedBy = username,
        };

        await _context.ErrorLogs.AddAsync(errorLog);
        await _context.SaveChangesAsync();
    }
}
