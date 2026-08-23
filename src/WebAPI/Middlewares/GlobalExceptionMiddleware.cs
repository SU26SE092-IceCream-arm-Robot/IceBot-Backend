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
        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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

                    var response = ApiResult<object>.Fail(GetSafeMessage(ex.StatusCode), ex.StatusCode);

                    if (ex.Errors != null)
                    {
                        foreach (var error in ex.Errors)
                        {
                            response.AddValidationError(error.Key, error.Value);
                        }
                    }

                    var httpStatusCode = Enum.IsDefined(typeof(HttpStatusCode), ex.StatusCode)
                        ? (HttpStatusCode)ex.StatusCode
                        : HttpStatusCode.BadRequest;

                    await WriteResponseAsync(context, response, httpStatusCode);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database update exception caught");

                    var response = ApiResult<object>.Fail("A database error occurred.", 500);
                    await WriteResponseAsync(context, response, HttpStatusCode.InternalServerError);
                }
                catch (DbException ex)
                {
                    _logger.LogError(ex, "Database exception caught");

                    var response = ApiResult<object>.Fail("A database error occurred.", 500);
                    await WriteResponseAsync(context, response, HttpStatusCode.InternalServerError);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "UnauthorizedAccessException caught in middleware");

                    var response = ApiResult<object>.Fail("Authentication is required.", (int)HttpStatusCode.Unauthorized);
                    await WriteResponseAsync(context, response, HttpStatusCode.Unauthorized);
                }
                catch (System.Collections.Generic.KeyNotFoundException ex)
                {
                    _logger.LogError(ex, "KeyNotFoundException caught in middleware");

                    var response = ApiResult<object>.Fail("Resource not found.", (int)HttpStatusCode.NotFound);
                    await WriteResponseAsync(context, response, HttpStatusCode.NotFound);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Operation was canceled");

                    var response = ApiResult<object>.Fail("Request was cancelled or timed out.", 408);
                    await WriteResponseAsync(context, response, HttpStatusCode.RequestTimeout);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception caught in middleware");

                    var response = ApiResult<object>.Fail("Internal Server Error.", (int)HttpStatusCode.InternalServerError);
                    await WriteResponseAsync(context, response, HttpStatusCode.InternalServerError);
                }
            }
        }

        private static string GetSafeMessage(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => "Request could not be completed.",
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "Access is denied.",
            StatusCodes.Status404NotFound => "Resource not found.",
            StatusCodes.Status409Conflict => "The requested operation conflicts with the current state.",
            StatusCodes.Status423Locked => "The requested resource is temporarily locked.",
            _ => "Request could not be completed."
        };

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
