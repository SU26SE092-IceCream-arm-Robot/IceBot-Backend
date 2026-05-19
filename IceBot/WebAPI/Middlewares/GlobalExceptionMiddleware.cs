using Application.Shared.Exceptions;
using Application.Shared.Wrappers;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;
using System.Data.Common;
using System.Net;
using System.Text.Json;

namespace WebAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IConfiguration _config;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IConfiguration config)
        {
            _next = next;
            _logger = logger;
            _config = config;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Items["X-Correlation-ID"]?.ToString() ?? context.TraceIdentifier;
            var path = context.Request.Path;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestPath", path))
            {
                try
                {
                    await _next(context);
                }
                catch (AppException ex) // App-defined (custom) exception
                {
                    _logger.LogError(ex, "AppException caught for path {Path} in middleware", path);

                    var response = ApiResult<object>.Fail(ex.Message, (int)HttpStatusCode.BadRequest);

                    if (ex.Errors != null)
                    {
                        foreach (var error in ex.Errors)
                        {
                            response.AddValidationError(error.Key, error.Value);
                        }
                    }

                    ApplyExceptionDetails(response, ex);
                    await WriteResponseAsync(context, response, HttpStatusCode.BadRequest);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database update exception caught");

                    var response = ApiResult<object>.Fail("A database error occurred.", 500)
                                                  .SetSystemError(ex.InnerException?.Message ?? ex.Message);

                    ApplyExceptionDetails(response, ex);
                    await WriteResponseAsync(context, response, HttpStatusCode.InternalServerError);
                }
                catch (DbException ex)
                {
                    _logger.LogError(ex, "Database exception caught");

                    var response = ApiResult<object>.Fail("Database error", 500)
                                                  .SetSystemError(ex.Message);

                    ApplyExceptionDetails(response, ex);
                    await WriteResponseAsync(context, response, HttpStatusCode.InternalServerError);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "UnauthorizedAccessException caught in middleware");

                    var response = ApiResult<object>.Fail("Unauthorized", (int)HttpStatusCode.Unauthorized)
                                                      .SetSystemError(ex.Message);

                    ApplyExceptionDetails(response, ex);
                    await WriteResponseAsync(context, response, HttpStatusCode.Unauthorized);
                }
                catch (KeyNotFoundException ex)
                {
                    _logger.LogError(ex, "KeyNotFoundException caught in middleware");

                    var response = ApiResult<object>.Fail("Resource not found", (int)HttpStatusCode.NotFound)
                                                      .SetSystemError(ex.Message);

                    ApplyExceptionDetails(response, ex);
                    await WriteResponseAsync(context, response, HttpStatusCode.NotFound);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Operation was canceled");

                    var response = ApiResult<object>.Fail("Request was cancelled or timed out", 408)
                                                  .SetSystemError(ex.Message);

                    ApplyExceptionDetails(response, ex);
                    await WriteResponseAsync(context, response, HttpStatusCode.RequestTimeout);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception caught in middleware");

                    var exposeStackTrace = _config.GetValue<bool>("ErrorHandling:ExposeStackTrace");
                    var response = ApiResult<object>.Fail("Internal Server Error", (int)HttpStatusCode.InternalServerError)
                                                      .SetSystemError(ex.Message);

                    ApplyExceptionDetails(response, ex);
                    await WriteResponseAsync(context, response, HttpStatusCode.InternalServerError);
                }
            }
        }

        private void ApplyExceptionDetails(ApiResult<object> response, Exception ex)
        {
            var exposeStackTrace = _config.GetValue<bool>("ErrorHandling:ExposeStackTrace");
            if (!exposeStackTrace) return;

            response.AddDetail("exceptionType", ex.GetType().FullName ?? "Exception")
                    .AddDetail("stackTrace", ex.StackTrace ?? string.Empty);
        }

        private static async Task WriteResponseAsync(HttpContext context, ApiResult<object> response, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);
        }
    }
}
