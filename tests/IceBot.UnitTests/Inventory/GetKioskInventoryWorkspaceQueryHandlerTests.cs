using Application.Identity.Tokens.Claims;
using Application.Inventory.Abstractions;
using Application.Inventory.Queries;
using Application.Inventory.Results;
using Domain.Catalog.Entities;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Inventory;

public sealed class GetKioskInventoryWorkspaceQueryHandlerTests
{
    [Fact]
    public async Task Returns_current_balances_active_refill_tasks_and_available_actions()
    {
        var kioskId = Guid.NewGuid();
        var kiosk = new Kiosk
        {
            Id = kioskId,
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid()
        };
        var inventoryId = Guid.NewGuid();
        var inventory = new KioskIngredientInventory
        {
            Id = inventoryId,
            KioskId = kioskId,
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            IngredientId = Guid.NewGuid(),
            Ingredient = new Ingredient { Code = "VANILLA-MIX", Name = "Vanilla mix" }
        };
        inventory.Configure("gram", 10, 10, null, InventoryTrackingMode.ManualEstimate, DateTimeOffset.UtcNow);

        var firstTask = new InventoryRefillTask
        {
            Id = Guid.NewGuid(),
            KioskId = kioskId,
            KioskIngredientInventoryId = inventoryId,
            RequestedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            Unit = "gram"
        };
        var duplicateActiveTask = new InventoryRefillTask
        {
            Id = Guid.NewGuid(),
            KioskId = kioskId,
            KioskIngredientInventoryId = inventoryId,
            RequestedAt = DateTimeOffset.UtcNow,
            Unit = "gram"
        };
        var store = Substitute.For<IInventoryWorkspaceStore>();
        store.GetKioskForInventoryTopologyAsync(kioskId, Arg.Any<CancellationToken>())
            .Returns(kiosk);
        store.ListKioskIngredientInventoriesAsync(kioskId, Arg.Any<CancellationToken>())
            .Returns([inventory]);
        store.ListActiveInventoryRefillTasksAsync(kioskId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([firstTask, duplicateActiveTask]);

        var result = await new GetKioskInventoryWorkspaceQueryHandler(store).HandleAsync(
            new GetKioskInventoryWorkspaceQuery(kioskId, new CurrentUserContext { IsSystemAdmin = true }));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.Summary.InventoryCount);
        Assert.Equal(1, result.Data.Summary.LowInventoryCount);
        Assert.Equal(2, result.Data.Summary.ActiveRefillTaskCount);
        Assert.True(result.Data.AvailableActions.CanManageRefill);
        Assert.True(result.Data.AvailableActions.CanAdjustInventory);
        Assert.True(result.Data.AvailableActions.CanConfigureInventory);
        Assert.Equal(KioskInventoryLevelStatus.Low, result.Data.Inventories.Single().InventoryStatus);
        Assert.Equal(firstTask.Id, result.Data.Inventories.Single().ActiveRefillTask!.Id);
    }

    [Fact]
    public async Task Rejects_inventory_workspace_outside_actor_scope()
    {
        var kioskId = Guid.NewGuid();
        var store = Substitute.For<IInventoryWorkspaceStore>();
        store.GetKioskForInventoryTopologyAsync(kioskId, Arg.Any<CancellationToken>())
            .Returns(new Kiosk
            {
                Id = kioskId,
                OrganizationId = Guid.NewGuid(),
                StoreId = Guid.NewGuid()
            });

        var result = await new GetKioskInventoryWorkspaceQueryHandler(store).HandleAsync(
            new GetKioskInventoryWorkspaceQuery(kioskId, new CurrentUserContext()));

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await store.DidNotReceive().ListKioskIngredientInventoriesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
