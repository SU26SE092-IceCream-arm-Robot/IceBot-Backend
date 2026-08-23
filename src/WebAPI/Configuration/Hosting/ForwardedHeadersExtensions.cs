using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace WebAPI.Configuration.Hosting;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddIceBotForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return services;
        }

        var trustedProxyNetworks = configuration
            .GetSection("ReverseProxy:TrustedNetworks")
            .Get<string[]>()?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray() ?? [];
        if (trustedProxyNetworks.Length == 0)
        {
            throw new InvalidOperationException(
                "ReverseProxy:TrustedNetworks is required in Production when HTTPS terminates at a reverse proxy.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var cidr in trustedProxyNetworks)
            {
                options.KnownIPNetworks.Add(ParseCidr(cidr));
            }
        });

        return services;
    }

    private static System.Net.IPNetwork ParseCidr(string cidr)
    {
        var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var address) ||
            !int.TryParse(parts[1], out var prefixLength))
        {
            throw new InvalidOperationException(
                $"ReverseProxy:TrustedNetworks contains an invalid CIDR: '{cidr}'.");
        }

        return new System.Net.IPNetwork(address, prefixLength);
    }
}
