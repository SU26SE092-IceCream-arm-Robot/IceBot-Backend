using System.Security.Cryptography;
using Application.Shared.Wrappers;
using Microsoft.Extensions.Options;
using WebAPI.Configuration.Security;

namespace WebAPI.Middlewares;

public sealed class ExecutionRequestBodyHashMiddleware
{
    public const string BodySha256ItemKey = "IceBot.ExecutionRequestBodySha256";
    private const int BufferThresholdBytes = 64 * 1024;
    private readonly RequestDelegate _next;
    private readonly int _maxRequestBodyBytes;

    public ExecutionRequestBodyHashMiddleware(
        RequestDelegate next,
        IOptions<ExecutionEndpointSecurityOptions> options)
    {
        _next = next;
        _maxRequestBodyBytes = options.Value.MaxRequestBodyBytes;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Value?.Contains("/iot/", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (_maxRequestBodyBytes <= 0)
            {
                throw new InvalidOperationException("ExecutionEndpointSecurity:MaxRequestBodyBytes must be positive.");
            }

            if (context.Request.ContentLength > _maxRequestBodyBytes)
            {
                await WritePayloadTooLargeAsync(context);
                return;
            }

            context.Request.EnableBuffering(BufferThresholdBytes, _maxRequestBodyBytes + 1L);
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[BufferThresholdBytes];
            var totalBytes = 0L;

            try
            {
                int bytesRead;
                while ((bytesRead = await context.Request.Body.ReadAsync(buffer, context.RequestAborted)) > 0)
                {
                    totalBytes += bytesRead;
                    if (totalBytes > _maxRequestBodyBytes)
                    {
                        context.Request.Body.Position = 0;
                        await WritePayloadTooLargeAsync(context);
                        return;
                    }

                    hasher.AppendData(buffer, 0, bytesRead);
                }
            }
            catch (IOException)
            {
                context.Request.Body.Position = 0;
                await WritePayloadTooLargeAsync(context);
                return;
            }

            context.Items[BodySha256ItemKey] = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
            context.Request.Body.Position = 0;
        }

        await _next(context);
    }

    private static Task WritePayloadTooLargeAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return context.Response.WriteAsJsonAsync(ApiResult<object>.Fail(
            "Execution request body exceeds the configured size limit.",
            StatusCodes.Status413PayloadTooLarge));
    }
}
