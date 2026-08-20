using Application.Devices.Catalog.Support;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Infrastructure.Catalog.Bootstrap;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Devices.Bootstrap;

/// <summary>
/// Creates an explicitly enabled demo inventory topology for the vanilla
/// soft-serve seed. It never overwrites operational quantities or tracking
/// choices after the first successful seed.
/// </summary>
public sealed class DevelopmentVanillaSoftServeTopologySeedHostedService : IHostedService
{
    private const string OrganizationCode = "ICEBOT-DEMO";
    private const string KioskCode = "ICEBOT-DEMO-KIOSK";
    private const string IngredientCode = "VANILLA-SOFT-SERVE-MIX";
    private const string DeviceTypeCode = "SOFT-SERVE-MACHINE";
    private const string DeviceModelCode = "SOFT-SERVE-HOPPER-V1";
    private const string DeviceCode = "ICEBOT-DEMO-SOFT-SERVE-MACHINE";
    private const string ContainerCode = "MIX_HOPPER";
    private const decimal InitialQuantity = 6000m;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<DevelopmentVanillaSoftServeTopologySeedHostedService> _logger;

    public DevelopmentVanillaSoftServeTopologySeedHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<DevelopmentVanillaSoftServeTopologySeedHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!IceBotDemoTenantSeedHostedService.IsEnabled(_configuration) ||
            !_configuration.GetValue<bool>("DemoCatalogSeed:SeedInventoryTopology"))
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var now = DateTimeOffset.UtcNow;
        var kiosk = await dbContext.Kiosks
            .Include(candidate => candidate.Store)
            .SingleOrDefaultAsync(candidate => candidate.Code == KioskCode, cancellationToken);
        var ingredient = await dbContext.Ingredients
            .WhereNotDeleted()
            .SingleOrDefaultAsync(candidate => candidate.Code == IngredientCode, cancellationToken);

        if (kiosk is null || ingredient is null)
        {
            _logger.LogDebug(
                "Skipped vanilla soft-serve topology seed because the demo kiosk or operational ingredient is unavailable.");
            return;
        }

        var isDemoKiosk = await dbContext.Organizations
            .AnyAsync(candidate => candidate.Id == kiosk.OrganizationId && candidate.Code == OrganizationCode, cancellationToken);
        if (!isDemoKiosk)
        {
            _logger.LogDebug("Skipped vanilla soft-serve topology seed because kiosk {KioskCode} is outside ICEBOT-DEMO.", KioskCode);
            return;
        }

        var balance = await dbContext.KioskIngredientInventories
            .SingleOrDefaultAsync(candidate => candidate.KioskId == kiosk.Id && candidate.IngredientId == ingredient.Id && candidate.Unit == "gram", cancellationToken);
        if (balance is null)
        {
            balance = new KioskIngredientInventory
            {
                Id = Guid.NewGuid(), OrganizationId = kiosk.OrganizationId, StoreId = kiosk.StoreId,
                KioskId = kiosk.Id, IngredientId = ingredient.Id, OriginNodeId = Guid.Empty,
                Version = 1, CreatedAt = now
            };
            balance.Configure("gram", InitialQuantity, null, null, InventoryTrackingMode.ManualEstimate, now);
            dbContext.KioskIngredientInventories.Add(balance);
            dbContext.StockMovements.Add(StockMovement.CreateForKioskInventory(
                balance.Id, balance.OrganizationId, balance.StoreId, balance.KioskId, balance.IngredientId,
                "InitialBalance", InitialQuantity, 0m, InitialQuantity, balance.Unit, now,
                "DEVELOPMENT_INITIAL_STOCK", "KioskIngredientInventory", balance.Id, isEstimated: true));
        }

        var deviceType = await EnsureDeviceTypeAsync(dbContext, now, cancellationToken);
        var deviceModel = await EnsureDeviceModelAsync(dbContext, deviceType.Id, now, cancellationToken);
        var device = await dbContext.Devices
            .SingleOrDefaultAsync(candidate => candidate.KioskId == kiosk.Id && candidate.Code == DeviceCode, cancellationToken);
        if (device is null)
        {
            device = Device.CreateProvisioning(
                deviceType.Id,
                deviceModel.Id,
                kiosk.Id,
                DeviceCode,
                "Demo soft-serve machine",
                null,
                "Soft-serve hopper",
                null,
                now);
            device.Id = Guid.NewGuid();
            device.CreatedAt = now;
            dbContext.Devices.Add(device);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var state = await dbContext.IngredientDispenserStates
            .SingleOrDefaultAsync(candidate => candidate.DeviceId == device.Id && candidate.ContainerCode == ContainerCode, cancellationToken);
        if (state is null)
        {
            state = new IngredientDispenserState
            {
                Id = Guid.NewGuid(), DeviceId = device.Id, KioskId = kiosk.Id, IngredientId = ingredient.Id,
                ContainerCode = ContainerCode, CurrentLevelStatus = IngredientLevelStatus.Full,
                EstimatedQuantity = InitialQuantity, CapacityQuantity = InitialQuantity, Unit = "gram",
                LastMeasuredAt = now, IsActive = true, OriginNodeId = Guid.Empty, Version = 1, CreatedAt = now
            };
            state.ChangeTrackingMode(InventoryTrackingMode.ManualEstimate);
            dbContext.IngredientDispenserStates.Add(state);
        }
        state.KioskIngredientInventoryId = balance.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Ensured Development vanilla soft-serve balance and optional hopper for kiosk {KioskCode}.", KioskCode);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<DeviceType> EnsureDeviceTypeAsync(
        IceBotDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.DeviceTypes
            .SingleOrDefaultAsync(candidate => candidate.Code == DeviceTypeCode, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var deviceType = new DeviceType
        {
            Code = DeviceTypeCode,
            Name = "Soft-serve machine",
            Description = "Development demo soft-serve hopper machine.",
            Category = "FoodPreparation",
            RequiresKioskAssignment = true,
            IsActive = true,
            CreatedAt = now
        };
        dbContext.DeviceTypes.Add(deviceType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return deviceType;
    }

    private static async Task<DeviceModel> EnsureDeviceModelAsync(
        IceBotDbContext dbContext,
        long deviceTypeId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.DeviceModels
            .SingleOrDefaultAsync(candidate => candidate.DeviceTypeId == deviceTypeId && candidate.Code == DeviceModelCode, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var deviceModel = new DeviceModel
        {
            DeviceTypeId = deviceTypeId,
            Code = DeviceModelCode,
            Name = "Soft-serve hopper v1",
            CapabilitiesJson = DeviceCapabilityContract.Serialize([DeviceCapabilityContract.IngredientDispenser]),
            CreatedAt = now
        };
        dbContext.DeviceModels.Add(deviceModel);
        await dbContext.SaveChangesAsync(cancellationToken);
        return deviceModel;
    }
}
