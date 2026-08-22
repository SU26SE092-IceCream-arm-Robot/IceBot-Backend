using Application.Inventory.Abstractions;
using Application.Inventory.Services;
using Domain.Catalog.Entities;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Inventory;

public sealed class InventoryReadinessEvaluatorTests
{
    [Fact]
    public async Task QuantifiedEvidence_RejectsExpiredOrInsufficientIngredient()
    {
        var store = Substitute.For<IInventoryStore>();
        var kiosk = new Kiosk { OrganizationId = Guid.NewGuid(), StoreId = Guid.NewGuid(), Code = "KIOSK-1", Name = "Kiosk 1" };
        var ingredient = new Ingredient { Code = "MILK", Name = "Milk", IsActive = true };
        var recipeId = Guid.NewGuid();
        var recipeItem = new RecipeItem
        {
            RecipeId = recipeId,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            Quantity = 10,
            Unit = "gram"
        };
        var device = Device.CreateProvisioning(1, null, kiosk.Id, "DISPENSER-1", "Dispenser 1", null, null, null, null);
        device.SetStatus(DeviceStatus.Online);
        var state = new IngredientDispenserState
        {
            DeviceId = device.Id,
            Device = device,
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            ContainerCode = "MILK-1",
            EstimatedQuantity = 5,
            Unit = "gram",
            LevelToQuantityProfileJson = "[]",
            LastMeasuredAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        store.GetKioskForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(kiosk);
        store.ListRequiredRecipeItemsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([recipeItem]);
        store.ListSupportedProductOptionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns([]);
        store.ListStatesForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns([state]);
        store.ListKioskIngredientInventoriesAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns([CreateBalance(kiosk, ingredient, 5m, InventoryTrackingMode.ManualEstimate)]);

        var result = await new InventoryReadinessEvaluator(store).EvaluateKioskAsync(
            kiosk.Id,
            [new InventoryReadinessRouteInput(Guid.NewGuid(), "ROUTE-1", Guid.NewGuid(), recipeId,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), kiosk.OrganizationId, null, null,
                kiosk.OrganizationId, null, null)],
            options: new InventoryReadinessEvaluationOptions(InventoryReadinessEvaluationPurpose.RuntimeSellability));

        Assert.NotNull(result);
        Assert.False(result.IsReady);
        Assert.Equal(InventoryReadinessStatus.QuantityInsufficient, result.OverallStatus);
        Assert.Equal(10, Assert.Single(result.Ingredients).RequiredQuantity);
    }

    [Fact]
    public async Task ZeroEstimate_DoesNotChangeTopologyReadinessV1()
    {
        var store = Substitute.For<IInventoryStore>();
        var kiosk = new Kiosk
        {
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            Code = "KIOSK-1",
            Name = "Kiosk 1"
        };
        var ingredient = new Ingredient
        {
            Code = "MILK",
            Name = "Milk",
            IsActive = true
        };
        var recipeId = Guid.NewGuid();
        var recipeItem = new RecipeItem
        {
            RecipeId = recipeId,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            Quantity = 10
        };
        var device = Device.CreateProvisioning(
            1, null, kiosk.Id, "DISPENSER-1", "Dispenser 1", null, null, null, null);
        device.SetStatus(DeviceStatus.Online);
        var state = new IngredientDispenserState
        {
            DeviceId = device.Id,
            Device = device,
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            ContainerCode = "MILK-1",
            EstimatedQuantity = 0,
            LevelToQuantityProfileJson = "[]",
            CurrentLevelStatus = IngredientLevelStatus.Low,
            IsActive = true
        };

        store.GetKioskForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>())
            .Returns(kiosk);
        store.ListRequiredRecipeItemsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(recipeId)),
                Arg.Any<CancellationToken>())
            .Returns([recipeItem]);
        store.ListSupportedProductOptionsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        store.ListStatesForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>())
            .Returns([state]);
        store.ListKioskIngredientInventoriesAsync(kiosk.Id, Arg.Any<CancellationToken>())
            .Returns([CreateBalance(kiosk, ingredient, 0m, InventoryTrackingMode.ManualEstimate)]);

        var result = await new InventoryReadinessEvaluator(store).EvaluateKioskAsync(
            kiosk.Id,
            [new InventoryReadinessRouteInput(
                Guid.NewGuid(),
                "ROUTE-1",
                Guid.NewGuid(),
                recipeId,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                kiosk.OrganizationId,
                null,
                null,
                kiosk.OrganizationId,
                null,
                null)]);

        Assert.NotNull(result);
        Assert.True(result.IsReady);
        Assert.Equal(InventoryReadinessStatus.Ready, result.OverallStatus);
        Assert.Equal(InventoryReadinessStatus.Ready, Assert.Single(result.Ingredients).Status);
    }

    [Fact]
    public async Task ManualEstimate_DoesNotRequireOnlineDeviceOrCalibration()
    {
        var store = Substitute.For<IInventoryStore>();
        var kiosk = new Kiosk { OrganizationId = Guid.NewGuid(), StoreId = Guid.NewGuid(), Code = "KIOSK-1", Name = "Kiosk 1" };
        var ingredient = new Ingredient { Code = "MIX", Name = "Mix", IsActive = true };
        var recipeId = Guid.NewGuid();
        var device = Device.CreateProvisioning(1, null, kiosk.Id, "MIXER", "Mixer", null, null, null, null);
        var state = new IngredientDispenserState
        {
            DeviceId = device.Id,
            Device = device,
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            ContainerCode = "MIX-HOPPER",
            EstimatedQuantity = 100,
            Unit = "gram",
            IsActive = true
        };
        store.GetKioskForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(kiosk);
        store.ListRequiredRecipeItemsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([
            new RecipeItem { RecipeId = recipeId, IngredientId = ingredient.Id, Ingredient = ingredient, Quantity = 10, Unit = "gram" }]);
        store.ListSupportedProductOptionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns([]);
        store.ListStatesForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns([state]);
        store.ListKioskIngredientInventoriesAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns([CreateBalance(kiosk, ingredient, 100m, InventoryTrackingMode.ManualEstimate)]);

        var result = await new InventoryReadinessEvaluator(store).EvaluateKioskAsync(kiosk.Id,
            [new InventoryReadinessRouteInput(Guid.NewGuid(), "ROUTE-1", Guid.NewGuid(), recipeId,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), kiosk.OrganizationId, null, null,
                kiosk.OrganizationId, null, null)],
            options: new InventoryReadinessEvaluationOptions(InventoryReadinessEvaluationPurpose.RuntimeSellability));

        Assert.True(result!.IsReady);
    }

    [Fact]
    public async Task SensorRequired_RejectsMissingSensorEvidence()
    {
        var store = Substitute.For<IInventoryStore>();
        var kiosk = new Kiosk { OrganizationId = Guid.NewGuid(), StoreId = Guid.NewGuid(), Code = "KIOSK-1", Name = "Kiosk 1" };
        var ingredient = new Ingredient { Code = "MIX", Name = "Mix", IsActive = true };
        var recipeId = Guid.NewGuid();
        var device = Device.CreateProvisioning(1, null, kiosk.Id, "SENSOR", "Sensor", null, null, null, null);
        device.SetStatus(DeviceStatus.Online);
        var state = new IngredientDispenserState
        {
            DeviceId = device.Id,
            Device = device,
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            ContainerCode = "MIX-HOPPER",
            EstimatedQuantity = 100,
            Unit = "gram",
            IsActive = true,
            LevelToQuantityProfileJson = "[]"
        };
        state.ChangeTrackingMode(InventoryTrackingMode.SensorRequired);
        store.GetKioskForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(kiosk);
        store.ListRequiredRecipeItemsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([
            new RecipeItem { RecipeId = recipeId, IngredientId = ingredient.Id, Ingredient = ingredient, Quantity = 10, Unit = "gram" }]);
        store.ListSupportedProductOptionsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns([]);
        store.ListStatesForInventoryTopologyAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns([state]);
        var balance = CreateBalance(kiosk, ingredient, 100m, InventoryTrackingMode.SensorRequired);
        state.KioskIngredientInventoryId = balance.Id;
        store.ListKioskIngredientInventoriesAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns([balance]);

        var result = await new InventoryReadinessEvaluator(store).EvaluateKioskAsync(kiosk.Id,
            [new InventoryReadinessRouteInput(Guid.NewGuid(), "ROUTE-1", Guid.NewGuid(), recipeId,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), kiosk.OrganizationId, null, null,
                kiosk.OrganizationId, null, null)],
            options: new InventoryReadinessEvaluationOptions(
                InventoryReadinessEvaluationPurpose.RuntimeSellability,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(5)));

        Assert.False(result!.IsReady);
        Assert.Equal(InventoryReadinessStatus.InventoryEvidenceStale, result.OverallStatus);
    }

    private static KioskIngredientInventory CreateBalance(Kiosk kiosk, Ingredient ingredient, decimal quantity, InventoryTrackingMode trackingMode)
    {
        var balance = new KioskIngredientInventory
        {
            Id = Guid.NewGuid(),
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            KioskId = kiosk.Id,
            IngredientId = ingredient.Id,
            Kiosk = kiosk,
            Ingredient = ingredient
        };
        balance.Configure("gram", quantity, null, null, trackingMode, DateTimeOffset.UtcNow);
        return balance;
    }
}
