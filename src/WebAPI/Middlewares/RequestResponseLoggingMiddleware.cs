using Application.Shared.Utils;
using Serilog.Context;
using System.Diagnostics;
using System.Text;

namespace WebAPI.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private const int MaxLoggedBodyLength = 1000;

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly IConfiguration _config;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger, IConfiguration config)
        {
            _next = next;
            _logger = logger;
            _config = config;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var traceId = context.TraceIdentifier;
            var exposeSensitiveData = _config.GetValue<bool>("Logging:ExposeSensitiveData");

            using (LogContext.PushProperty("TraceId", traceId))
            {
                var stopwatch = Stopwatch.StartNew();

                var requestBody = await ReadRequestBodyAsync(context);
                var loggedRequest = exposeSensitiveData
                    ? Truncate(requestBody, MaxLoggedBodyLength)
                    : Truncate(SensitiveDataMasker.MaskSensitiveData(requestBody), MaxLoggedBodyLength);

                _logger.LogInformation("Incoming Request: {method} {url} | Body: {body}",
                    context.Request.Method, context.Request.Path, loggedRequest);

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
                var loggedResponse = exposeSensitiveData
                    ? Truncate(responseBody, MaxLoggedBodyLength)
                    : Truncate(SensitiveDataMasker.MaskSensitiveData(responseBody), MaxLoggedBodyLength);

                _logger.LogInformation("Response: {statusCode} | Duration: {duration}ms | Body: {body}",
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
    }
}
