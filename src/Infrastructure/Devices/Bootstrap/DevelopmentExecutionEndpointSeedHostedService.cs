using Domain.Devices.ExecutionEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Infrastructure.Data;

namespace Infrastructure.Devices.Bootstrap;

/// <summary>
/// Creates a visible local Full Edge endpoint for UI/readiness testing only.
/// It intentionally remains Provisioning because no local Edge runtime or credential exists.
/// </summary>
public sealed class DevelopmentExecutionEndpointSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    IConfiguration configuration,
    ILogger<DevelopmentExecutionEndpointSeedHostedService> logger) : IHostedService
{
    private const string DemoKioskCode = "ICEBOT-DEMO-KIOSK";
    private const string EndpointCode = "ICEBOT-DEMO-EDGE";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment() ||
            !configuration.GetValue<bool>("DevelopmentExecutionEndpointSeed:Enabled"))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var kiosk = await dbContext.Kiosks.SingleOrDefaultAsync(
            candidate => candidate.DeletedAt == null && candidate.Code == DemoKioskCode,
            cancellationToken);
        if (kiosk is null)
        {
            logger.LogInformation("Skipped development execution endpoint seed because kiosk {KioskCode} is unavailable.",
                DemoKioskCode);
            return;
        }

        var existing = await dbContext.KioskExecutionEndpoints.AnyAsync(
            candidate => candidate.KioskId == kiosk.Id && candidate.EndpointCode == EndpointCode,
            cancellationToken);
        if (existing)
        {
            return;
        }

        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            EndpointCode,
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        dbContext.KioskExecutionEndpoints.Add(endpoint);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded development execution endpoint {EndpointCode}; it remains Provisioning until an Edge runtime is provisioned.",
            EndpointCode);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
