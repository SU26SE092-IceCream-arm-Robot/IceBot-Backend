using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Catalog.Bootstrap;

/// <summary>
/// Creates the isolated ICEBOT-DEMO tenant fixture when explicitly enabled.
/// It never modifies any tenant other than the fixture identified by its code.
/// </summary>
public sealed class IceBotDemoTenantSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<IceBotDemoTenantSeedHostedService> logger) : IHostedService
{
    public const string OrganizationCode = "ICEBOT-DEMO";
    public const string StoreCode = "ICEBOT-DEMO-STORE";
    public const string KioskCode = "ICEBOT-DEMO-KIOSK";

    public static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("DemoCatalogSeed:IceBotDemoEnabled");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled(configuration))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var now = DateTimeOffset.UtcNow;
        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(candidate => candidate.Code == OrganizationCode, cancellationToken);
        if (organization is null)
        {
            organization = new Organization
            {
                Code = OrganizationCode,
                Name = "IceBot Demo Organization",
                Status = EntityStatus.Active,
                CreatedAt = now
            };
            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var store = await dbContext.Stores
            .SingleOrDefaultAsync(candidate => candidate.OrganizationId == organization.Id && candidate.Code == StoreCode, cancellationToken);
        if (store is null)
        {
            store = new Store
            {
                OrganizationId = organization.Id,
                Code = StoreCode,
                Name = "IceBot Demo Store",
                Status = EntityStatus.Active,
                CreatedAt = now
            };
            dbContext.Stores.Add(store);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var kioskExists = await dbContext.Kiosks.AnyAsync(
            candidate => candidate.OrganizationId == organization.Id && candidate.StoreId == store.Id && candidate.Code == KioskCode,
            cancellationToken);
        if (!kioskExists)
        {
            dbContext.Kiosks.Add(new Kiosk
            {
                OrganizationId = organization.Id,
                StoreId = store.Id,
                Code = KioskCode,
                Name = "IceBot Demo Kiosk",
                CreatedAt = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Ensured {OrganizationCode} demo organization, store, and kiosk fixture.", OrganizationCode);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
