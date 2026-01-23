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
            Timestamp = DateTime.Now,
            Path = context.Request.Path,
            Method = context.Request.Method,
        };

        HttpStatusCode statusCode;

        var correlationId = CorrelationContext.CorrelationId;

        switch (exception)
        {
            case SapApiExceptionDto sapEx:
                statusCode = HttpStatusCode.BadGateway;
                errorResponse.Message = sapEx.Message;
                errorResponse.ErrorCode = ErrorCodes.SapError;
                break;

            case DbUpdateException dbEx:
                statusCode = HttpStatusCode.Conflict;
                errorResponse.Message =
                    "Database update failed"
                    + (string.IsNullOrWhiteSpace(dbEx.Message) ? "" : $": {dbEx.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(dbEx.InnerException?.Message)
                            ? $"; {dbEx.InnerException.Message}"
                            : ""
                    );

                errorResponse.ErrorCode = ErrorCodes.DatabaseUpdate;

                if (dbEx.InnerException is SqlException sqlEx)
                {
                    errorResponse.Details = HandleSqlException(sqlEx);
                }
                break;

            case SqlException sqEx:
                statusCode = HttpStatusCode.Conflict;
                errorResponse.Message =
                    "Database error occurred"
                    + (string.IsNullOrWhiteSpace(sqEx.Message) ? "" : $": {sqEx.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(sqEx.InnerException?.Message)
                            ? $"; {sqEx.InnerException.Message}"
                            : ""
                    );

                errorResponse.ErrorCode = ErrorCodes.Database;
                errorResponse.Details = HandleSqlException(sqEx);
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                errorResponse.Message = "Not authorized";
                errorResponse.ErrorCode = ErrorCodes.UnAuthorize;
                break;

            case ValidationExceptionDto validationEx:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse.Message = validationEx.Message;
                errorResponse.ErrorCode = ErrorCodes.Validation;
                break;

            case CustomerSyncException custEx:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse.Message = custEx.Message;
                errorResponse.ErrorCode = ErrorCodes.CustomerSync;
                break;

            case MaterialSyncException matEx:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse.Message = matEx.Message;
                errorResponse.ErrorCode = ErrorCodes.MaterialSync;
                break;

            case ReceiptSyncException receiptEx:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse.Message = receiptEx.Message;
                errorResponse.ErrorCode = ErrorCodes.Unexpected;

                break;

            case MaterialStockSyncException stockEx:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse.Message = stockEx.Message;
                errorResponse.ErrorCode = ErrorCodes.Unexpected;

                break;

            case BusinessUnitResolveException buEx:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse.Message = buEx.Message;
                errorResponse.ErrorCode = ErrorCodes.Validation;
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                errorResponse.Message =
                    "An unexpected error occurred"
                    + (string.IsNullOrWhiteSpace(exception.Message) ? "" : $": {exception.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(exception.InnerException?.Message)
                            ? $"; {exception.InnerException.Message}"
                            : ""
                    );
                errorResponse.ErrorCode = ErrorCodes.Unexpected;
                _logger.LogError(
                    exception,
                    "Unhandled exception | CorrelationId: {CorrelationId} | Path: {Path} | ErrorCode: {ErrorCode}",
                    correlationId,
                    context.Request.Path,
                    errorResponse.ErrorCode
                );
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        errorResponse.CorrelationId = correlationId;

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
