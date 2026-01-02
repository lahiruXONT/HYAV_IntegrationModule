using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Integration.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(  RequestDelegate next,ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var response = context.Response;

        var errorResponse = new ErrorResponse
        {
            Success = false,
            Message = GetUserFriendlyMessage(exception),
            Timestamp = DateTime.Now
        };

        response.StatusCode = exception switch
        {
            SapApiExceptionDto => (int)HttpStatusCode.BadGateway,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ValidationExceptionDto => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        errorResponse.ErrorType = exception.GetType().Name;

        _logger.LogError(exception, "Error : {ErrorType}, Message: {Message}, Path: {Path}, StatusCode: {StatusCode}", errorResponse.ErrorType, exception.Message, context.Request.Path, response.StatusCode);

        var result = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(result);
    }

    private string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            SapApiExceptionDto => "SAP system Connection failed.",
            DbUpdateException => "Database update failed.",
            UnauthorizedAccessException => "Not authorized.",
            ValidationExceptionDto => exception.Message,
            _ => "An unexpected error occurred."
        };
    }

}