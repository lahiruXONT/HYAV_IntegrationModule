using System.Net;
using System.Text.Json;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Integration.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env
    )
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Success = false,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            Method = context.Request.Method,
        };

        switch (exception)
        {
            case SapApiExceptionDto sapEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                errorResponse.Message = "SAP system connection failed";
                errorResponse.ErrorCode = ErrorCodes.SapError;
                break;

            case DbUpdateException dbEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Message = "Database update failed";
                errorResponse.ErrorCode = ErrorCodes.DatabaseUpdate;

                if (dbEx.InnerException is SqlException sqlEx)
                {
                    errorResponse.Details = HandleSqlException(sqlEx);
                }
                break;

            case SqlException sqEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Message = "Database error occurred";
                errorResponse.ErrorCode = ErrorCodes.Database;
                errorResponse.Details = HandleSqlException(sqEx);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "Not authorized";
                errorResponse.ErrorCode = ErrorCodes.UnAuthorize;
                break;

            case ValidationExceptionDto validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = validationEx.Message;
                errorResponse.ErrorCode = ErrorCodes.Validation;
                break;

            case CustomerSyncException custEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = $"Customer sync failed for {custEx.CustomerCode}";
                errorResponse.ErrorCode = ErrorCodes.CustomerSync;
                errorResponse.CustomerCode = custEx.CustomerCode;
                break;

            case MaterialSyncException matEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = $"Material sync failed for {matEx.MaterialCode}";
                errorResponse.ErrorCode = ErrorCodes.MaterialSync;
                errorResponse.MaterialCode = matEx.MaterialCode;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred";
                errorResponse.ErrorCode = ErrorCodes.Unexpected;
                break;
        }

        var correlationId = CorrelationContext.CorrelationId;

        _logger.LogError(
            exception,
            "Error {ErrorCode}: {Message} | CorrelationId: {CorrelationId} | Path: {Path}",
            errorResponse.ErrorCode,
            exception.Message,
            correlationId,
            context.Request.Path
        );

        if (_env.IsDevelopment())
        {
            errorResponse.StackTrace = exception.StackTrace;
            errorResponse.InnerException = exception.InnerException?.Message;
        }

        var result = JsonSerializer.Serialize(
            errorResponse,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _env.IsDevelopment(),
            }
        );

        await context.Response.WriteAsync(result);
    }

    private List<string> HandleSqlException(SqlException ex)
    {
        var details = new List<string>();

        if (ex.Number == 2601 || ex.Number == 2627)
            details.Add("Duplicate record found");
        else if (ex.Number == 547)
            details.Add("Foreign key constraint violation");
        else if (ex.Number == 1205)
            details.Add("Deadlock occurred");
        else if (ex.Number == 4060)
            details.Add("Cannot open database");
        else
            details.Add($"SQL Error {ex.Number}: {ex.Message}");

        return details;
    }
}
