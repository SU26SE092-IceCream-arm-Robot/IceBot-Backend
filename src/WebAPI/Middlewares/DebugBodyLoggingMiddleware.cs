using Application.Shared.Utils;
using Serilog.Context;
using System.Diagnostics;
using System.Text;

namespace WebAPI.Middlewares;

public class DebugBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DebugBodyLoggingMiddleware> _logger;
    private readonly IConfiguration _config;

    public DebugBodyLoggingMiddleware(
        RequestDelegate next,
        ILogger<DebugBodyLoggingMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Check if debug logging is enabled at all
        var debugLoggingEnabled = _config.GetValue("Observability:DebugBodyLogging:Enabled", false);
        if (!debugLoggingEnabled || ShouldSkipEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var logRequest = _config.GetValue("Observability:DebugBodyLogging:LogRequestBody", true);
        var logResponse = _config.GetValue("Observability:DebugBodyLogging:LogResponseBody", false);
        var maxBodyLength = _config.GetValue("Observability:DebugBodyLogging:MaxBodyLength", 1000);

        var traceId = context.TraceIdentifier;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            var stopwatch = Stopwatch.StartNew();

            if (logRequest)
            {
                var requestBody = await ReadRequestBodyAsync(context);
                var loggedRequest = Truncate(SensitiveDataMasker.MaskSensitiveData(requestBody), maxBodyLength);

                _logger.LogInformation("Incoming Request (Debug): {method} {url} | Body: {body}",
                    context.Request.Method, context.Request.Path, loggedRequest);
            }

            if (!logResponse)
            {
                // If not logging response, just call next and finish
                await _next(context);
                return;
            }

            // We are logging the response
            var originalBodyStream = context.Response.Body;
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            catch
            {
                context.Response.Body = originalBodyStream;
                responseBodyStream.Dispose();
                throw;
            }

            stopwatch.Stop();

            var responseBody = await ReadResponseBodyAsync(context);
            var loggedResponse = Truncate(SensitiveDataMasker.MaskSensitiveData(responseBody), maxBodyLength);

            _logger.LogInformation("Response (Debug): {statusCode} | Duration: {duration}ms | Body: {body}",
                context.Response.StatusCode, stopwatch.ElapsedMilliseconds, loggedResponse);

            context.Response.Body = originalBodyStream;
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
            responseBodyStream.Dispose();
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        if (!HasLoggableContentType(context.Request.ContentType))
        {
            return string.Empty;
        }

        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        return body;
    }

    private async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        if (!HasLoggableContentType(context.Response.ContentType))
        {
            return string.Empty;
        }

        context.Response.Body.Seek(0, SeekOrigin.Begin);

        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        return body;
    }

    private static bool HasLoggableContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("application/problem+json", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.Length <= maxLength ? input : input.Substring(0, maxLength) + "...(truncated)";
    }

    private static bool ShouldSkipEndpoint(PathString path)
    {
        if (!path.HasValue) return false;

        var p = path.Value;
        return p.StartsWith("/api/v1/authentication", StringComparison.OrdinalIgnoreCase) ||
               p.Contains("/password", StringComparison.OrdinalIgnoreCase) ||
               p.Contains("/webhook", StringComparison.OrdinalIgnoreCase);
    }
}
