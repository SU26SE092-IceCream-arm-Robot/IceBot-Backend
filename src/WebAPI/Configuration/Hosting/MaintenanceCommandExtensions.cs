using Infrastructure.Catalog.Bootstrap;
using Serilog;

namespace WebAPI.Configuration.Hosting;

public static class MaintenanceCommandExtensions
{
    public static async Task<bool> TryRunIceBotMaintenanceCommandAsync(this WebApplication app, string[] args)
    {
        if (args.Contains("--delete-legacy-automation-fixture", StringComparer.OrdinalIgnoreCase))
        {
            EnsureDevelopment(app, "Legacy automation fixture deletion");
            await using var scope = app.Services.CreateAsyncScope();
            var reset = scope.ServiceProvider.GetRequiredService<DevelopmentIceBotDemoReset>();
            var deleted = await reset.DeleteLegacyAutomationFixtureAsync(CancellationToken.None);
            Log.Information("Deleted legacy ICEBOT-AUTOMATION-TEST fixture organization: {Deleted}.", deleted);
            return true;
        }

        if (args.Contains("--reset-icebot-demo", StringComparer.OrdinalIgnoreCase))
        {
            EnsureDevelopment(app, "ICEBOT-DEMO reset");
            await using var scope = app.Services.CreateAsyncScope();
            var reset = scope.ServiceProvider.GetRequiredService<DevelopmentIceBotDemoReset>();
            var result = await reset.ResetAsync(CancellationToken.None);
            Log.Information(
                "Reset {OrganizationCode} ({OrganizationId}): {Imports} imports, {Artifacts} artifacts, {Programs} programs, {Contracts} contracts, {Bindings} bindings, {Releases} releases, {MenuItems} menu items, {Objects} objects deleted, {RetainedObjects} objects retained. Deleted legacy automation fixture: {DeletedAutomationFixture}.",
                DevelopmentIceBotDemoReset.OrganizationCode, result.OrganizationId,
                result.DeletedImportCount, result.DeletedArtifactCount, result.DeletedProgramCount,
                result.DeletedContractCount, result.DeletedBindingCount, result.DeletedReleaseCount,
                result.DeletedMenuItemCount, result.DeletedObjectCount, result.RetainedObjectCount,
                result.DeletedAutomationFixture);
            return true;
        }

        if (args.Contains("--repair-icebot-demo-runtime", StringComparer.OrdinalIgnoreCase))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var repair = scope.ServiceProvider.GetRequiredService<IceBotDemoRuntimeRepair>();
            await repair.RepairAsync(CancellationToken.None);
            Log.Information("Repaired ICEBOT-DEMO runtime catalog and inventory fixture.");
            return true;
        }

        return false;
    }

    private static void EnsureDevelopment(WebApplication app, string commandName)
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException($"{commandName} is available only in Development.");
        }
    }
}
