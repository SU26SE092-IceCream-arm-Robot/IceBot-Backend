using System.Threading.RateLimiting;
using Application.ClientDevices.Contracts;
using Application.ClientDevices.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Configuration.Hosting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddIceBotRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api/v1/client-device-sessions"))
                {
                    return RateLimitPartition.GetNoLimiter("non-client-device-session");
                }

                return FixedWindow(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", 10, TimeSpan.FromMinutes(1));
            });
            options.AddPolicy("service-registration-submission", context =>
                FixedWindow(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", 5, TimeSpan.FromMinutes(10)));
            options.AddPolicy("client-device-session", context =>
                FixedWindow(context.Request.Headers[ClientDeviceSessionHeaderNames.ClientDeviceId].ToString(), 10, TimeSpan.FromMinutes(1)));
            options.AddPolicy("client-device-menu", context =>
            {
                var partition = context.User.FindFirst(ClientDeviceAuthenticationDefaults.ClientDeviceIdClaim)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";
                return FixedWindow(partition, 120, TimeSpan.FromMinutes(1));
            });
            options.AddPolicy("client-device-order", context =>
            {
                var deviceId = context.User.FindFirst(ClientDeviceAuthenticationDefaults.ClientDeviceIdClaim)?.Value ?? "unknown";
                var kioskId = context.User.FindFirst(ClientDeviceAuthenticationDefaults.KioskIdClaim)?.Value ?? "unknown";
                return FixedWindow($"{deviceId}:{kioskId}", 12, TimeSpan.FromMinutes(1));
            });
            options.AddPolicy("client-device-payment", context =>
            {
                var deviceId = context.User.FindFirst(ClientDeviceAuthenticationDefaults.ClientDeviceIdClaim)?.Value ?? "unknown";
                var orderId = context.Request.RouteValues["orderId"]?.ToString() ?? "unknown";
                return FixedWindow($"{deviceId}:{orderId}", 8, TimeSpan.FromMinutes(1));
            });
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(string partitionKey, int permitLimit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true
        });
}
