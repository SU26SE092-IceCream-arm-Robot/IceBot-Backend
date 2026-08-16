using Application.Devices.Telemetry;
using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.SalesCatalog.Availability;
using Application.SalesCatalog.ReadModels;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Inventory.Enums;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class MachineProductionInventoryGateTests
{
    [Fact]
    public async Task UnconfiguredInventoryTopology_DoesNotBlockMachineSale()
    {
        var inventory = Substitute.For<IInventoryReadinessEvaluator>();
        inventory.EvaluateKioskAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<InventoryReadinessRouteInput>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<InventoryReadinessEvaluationOptions>())
            .Returns(new KioskInventoryReadinessResult
            {
                HasConfiguredInventoryTopology = false,
                IsReady = false,
                OverallStatus = InventoryReadinessStatus.MissingIngredient
            });

        var result = await CreateGate(inventory).EvaluateAsync(
            CreateKiosk(),
            CreateMenuItem(),
            new ActiveProductionRouteOptionPolicy(Guid.NewGuid(), new HashSet<string>()),
            1,
            [],
            DateTimeOffset.UtcNow);

        Assert.True(result.CanSell);
    }

    [Fact]
    public async Task ConfiguredInventoryTopology_StillBlocksMissingIngredient()
    {
        var inventory = Substitute.For<IInventoryReadinessEvaluator>();
        inventory.EvaluateKioskAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<InventoryReadinessRouteInput>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<InventoryReadinessEvaluationOptions>())
            .Returns(new KioskInventoryReadinessResult
            {
                HasConfiguredInventoryTopology = true,
                IsReady = false,
                OverallStatus = InventoryReadinessStatus.MissingIngredient
            });

        var result = await CreateGate(inventory).EvaluateAsync(
            CreateKiosk(),
            CreateMenuItem(),
            new ActiveProductionRouteOptionPolicy(Guid.NewGuid(), new HashSet<string>()),
            1,
            [],
            DateTimeOffset.UtcNow);

        Assert.False(result.CanSell);
    }

    private static MachineProductionInventoryGate CreateGate(IInventoryReadinessEvaluator inventory) =>
        new(inventory, Options.Create(new EdgeTelemetryIngestionOptions()));

    private static Kiosk CreateKiosk() => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        StoreId = Guid.NewGuid(),
        Code = "KIOSK-1",
        Name = "Kiosk 1"
    };

    private static MenuItem CreateMenuItem()
    {
        var product = new Product { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Code = "PRODUCT", Name = "Product" };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Code = "VANILLA",
            Name = "Vanilla",
            FulfillmentType = FulfillmentType.MachineProduced
        };
        var recipe = new Recipe { Id = Guid.NewGuid(), ProductVariantId = variant.Id, Code = "RECIPE", Name = "Recipe" };
        return new MenuItem
        {
            ProductId = product.Id,
            ProductVariantId = variant.Id,
            RecipeId = recipe.Id,
            Code = "MENU-ITEM",
            DisplayName = "Vanilla",
            Product = product,
            ProductVariant = variant,
            Recipe = recipe
        };
    }
}
