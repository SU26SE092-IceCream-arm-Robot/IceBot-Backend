using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Application.Abstractions.Realtime;
using WebAPI.SignalR.Hubs;

namespace WebAPI.SignalR;

public static class SignalREndpointExtensions
{
    public static IServiceCollection AddIceBotSignalR(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(null, allowIntegerValues: false));
            });

        services.AddScoped<IRealtimeNotificationPublisher, SignalRNotificationPublisher>();
        return services;
    }

    public static IEndpointRouteBuilder MapIceBotSignalR(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<OrderHub>("/hubs/orders");
        endpoints.MapHub<OperationsHub>("/hubs/operations");
        endpoints.MapHub<ManagementDashboardHub>("/hubs/management-dashboard");
        return endpoints;
    }
}
