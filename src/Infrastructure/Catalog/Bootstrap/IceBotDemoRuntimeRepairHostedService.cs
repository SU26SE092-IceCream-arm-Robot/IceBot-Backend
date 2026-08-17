using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Catalog.Bootstrap;

/// <summary>
/// Applies the opt-in ICEBOT-DEMO runtime repair after the demo tenant, catalog,
/// and topology seed services have completed. Repair failures are logged without
/// preventing the API host from becoming available.
/// </summary>
public sealed class IceBotDemoRuntimeRepairHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<IceBotDemoRuntimeRepairHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("DemoCatalogSeed:RepairExistingDataOnStartup"))
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repair = scope.ServiceProvider.GetRequiredService<IceBotDemoRuntimeRepair>();
            var repaired = await repair.RepairAsync(cancellationToken, requireExistingFixture: false);
            logger.LogInformation(
                repaired
                    ? "Repaired ICEBOT-DEMO runtime catalog and inventory fixture after startup seeds."
                    : "Skipped ICEBOT-DEMO runtime repair because the fixture does not exist after startup seeds.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "ICEBOT-DEMO runtime repair failed after startup seeds. The API host will remain available.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
