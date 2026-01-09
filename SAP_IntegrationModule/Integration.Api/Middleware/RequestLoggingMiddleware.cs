using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Azure;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Polly;

namespace Integration.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider
    )
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = $"API-{Guid.NewGuid()}";
        CorrelationContext.CorrelationId = correlationId;

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["RequestPath"] = context.Request.Path,
                    ["RequestMethod"] = context.Request.Method,
                }
            )
        )
        {
            var endpoint = context.GetEndpoint();
            var controllerName = endpoint
                ?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                ?.ControllerName;

            if (string.Equals(controllerName, "Auth", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            long requestLogId = 0;

            var originalBody = context.Response.Body;
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                requestLogId = await LogRequest(context);

                await _next(context);

                stopwatch.Stop();
                if (context.Response.StatusCode >= 400)
                {
                    var responseText = await ReadResponseBody(context.Response);
                    await LogErrorToDatabase(context, requestLogId, responseText);
                }

                _logger.LogInformation(
                    "Request completed: {Method} {Path} with status {StatusCode} in {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch
            {
                stopwatch.Stop();
                throw;
            }
            finally
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBody);
                context.Response.Body = originalBody;
            }
        }
    }

    private async Task<long> LogRequest(HttpContext context)
    {
        try
        {
            var businessUnit =
                context.User?.FindFirst(ClaimTypes.System)?.Value
                ?? _configuration["DefaultBusinessUnit"]
                ?? "";

            var username = context.User?.Identity?.Name ?? "ANONYMOUS";
            var methodName = $"{context.Request.Method} {context.Request.Path}";

            var requestBody = await GetRequestBody(context.Request);
            var message = $"{requestBody}";

            using var scope = _serviceProvider.CreateScope();
            var _logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
            _logger.LogInformation(
                "Request received: {Method} {Path} by {User} in {BusinessUnit}",
                context.Request.Method,
                context.Request.Path,
                username,
                businessUnit
            );
            var requestLogId = await _logRepository.LogRequestAsync(
                businessUnit,
                username,
                methodName,
                message,
                "I"
            );

            return requestLogId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log received request to database.");
            return 0;
        }
    }

    private async Task<string> GetRequestBody(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0)
            return "[Empty Body]";

        try
        {
            request.EnableBuffering();

            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true
            );

            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
        catch
        {
            return "[Unable to read body]";
        }
    }

    private async Task LogErrorToDatabase(
        HttpContext context,
        Int64 requestDBLogId,
        string responseBody
    )
    {
        try
        {
            var businessUnit =
                context.User?.FindFirst(ClaimTypes.System)?.Value
                ?? _configuration["DefaultBusinessUnit"]
                ?? "";
            var username = context.User?.Identity?.Name ?? "";
            var methodName = $"{context.Request.Method} {context.Request.Path}";

            using var scope = _serviceProvider.CreateScope();
            var _logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
            await _logRepository.LogErrorAsync(
                businessUnit,
                username,
                methodName,
                responseBody,
                requestDBLogId,
                "E"
            );
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "Failed to log error to database.");
        }
    }

    private async Task<string> ReadResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return string.IsNullOrWhiteSpace(body) ? "[Empty Response]" : body;
    }
}
