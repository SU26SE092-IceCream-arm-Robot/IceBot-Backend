using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace IceBot.UnitTests.Inventory;

public sealed class KioskIngredientInventoryTests
{
    [Fact]
    public void Sensor_delta_reconciles_only_the_changed_hopper_contribution()
    {
        var inventory = CreateInventory(100m);
        var now = DateTimeOffset.UtcNow;

        inventory.ReconcileSensorDelta(40m, 40m, now);
        inventory.ReconcileSensorDelta(30m, 20m, now.AddMinutes(1));

        Assert.Equal(110m, inventory.EstimatedQuantity);
    }

    [Fact]
    public void First_sensor_observation_is_a_baseline_not_an_inventory_overwrite()
    {
        var inventory = CreateInventory(100m);

        inventory.ReconcileSensorDelta(40m, null, DateTimeOffset.UtcNow);

        Assert.Equal(100m, inventory.EstimatedQuantity);
    }

    private static KioskIngredientInventory CreateInventory(decimal quantity)
    {
        var inventory = new KioskIngredientInventory
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            KioskId = Guid.NewGuid(),
            IngredientId = Guid.NewGuid()
        };
        inventory.Configure("gram", quantity, null, null, InventoryTrackingMode.SensorAssisted, DateTimeOffset.UtcNow);
        return inventory;
    }
}
