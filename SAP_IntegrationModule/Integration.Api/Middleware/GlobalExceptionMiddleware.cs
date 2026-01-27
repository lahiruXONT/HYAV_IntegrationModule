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

        HttpStatusCode statusCode;
        string message;
        string errorCode = ErrorCodes.Unexpected;
        List<string>? details = null;

        var correlationId = CorrelationContext.CorrelationId;

        switch (exception)
        {
            case SapApiExceptionDto sapEx:
                statusCode = HttpStatusCode.BadGateway;
                message = sapEx.Message;
                errorCode = ErrorCodes.SapError;
                break;

            case DbUpdateException dbEx:
                statusCode = HttpStatusCode.Conflict;
                message =
                    "Database update failed"
                    + (string.IsNullOrWhiteSpace(dbEx.Message) ? "" : $": {dbEx.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(dbEx.InnerException?.Message)
                            ? $"; {dbEx.InnerException.Message}"
                            : ""
                    );

                errorCode = ErrorCodes.DatabaseUpdate;

                if (dbEx.InnerException is SqlException sqlEx)
                {
                    details = HandleSqlException(sqlEx);
                }
                break;

            case SqlException sqEx:
                statusCode = HttpStatusCode.Conflict;
                message =
                    "Database error occurred"
                    + (string.IsNullOrWhiteSpace(sqEx.Message) ? "" : $": {sqEx.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(sqEx.InnerException?.Message)
                            ? $"; {sqEx.InnerException.Message}"
                            : ""
                    );

                errorCode = ErrorCodes.Database;
                details = HandleSqlException(sqEx);
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "Not authorized";
                errorCode = ErrorCodes.UnAuthorize;
                break;

            case ValidationExceptionDto validationEx:
                statusCode = HttpStatusCode.BadRequest;
                message = validationEx.Message;
                errorCode = ErrorCodes.Validation;
                break;

            case IntegrationException intEx:
                statusCode = HttpStatusCode.InternalServerError;
                message = intEx.Message;
                errorCode = intEx.ErrorCode;
                break;

            case BusinessUnitResolveException buEx:
                statusCode = HttpStatusCode.InternalServerError;
                message = buEx.Message;
                errorCode = ErrorCodes.BusinessUnitResolve;
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                message =
                    "An unexpected error occurred"
                    + (string.IsNullOrWhiteSpace(exception.Message) ? "" : $": {exception.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(exception.InnerException?.Message)
                            ? $"; {exception.InnerException.Message}"
                            : ""
                    );
                errorCode = ErrorCodes.Unexpected;
                _logger.LogError(
                    exception,
                    "Unhandled exception | CorrelationId: {CorrelationId} | Path: {Path} | ErrorCode: {ErrorCode}",
                    correlationId,
                    context.Request.Path,
                    errorCode
                );
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new ApiResponse<ErrorResponseData>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Data = new ErrorResponseData
            {
                Timestamp = DateTime.Now,
                Path = context.Request.Path,
                Method = context.Request.Method,
                CorrelationId = CorrelationContext.CorrelationId,
                Details = details,
            },
        };

        var result = JsonSerializer.Serialize(
            response,
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

        switch (ex.Number)
        {
            case 2601:
            case 2627:
                details.Add("Duplicate record found");
                break;
            case 547:
                details.Add("Foreign key constraint violation");
                break;
            case 1205:
                details.Add("Deadlock occurred");
                break;
            case 4060:
                details.Add("Cannot open database");
                break;
            case 18456:
                details.Add("Login failed");
                break;
            default:
                details.Add($"SQL Error {ex.Number}: {ex.Message}");
                break;
        }

        return details;
    }
}
