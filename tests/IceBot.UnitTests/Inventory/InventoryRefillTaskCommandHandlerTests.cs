using System.Security.Cryptography;
using System.Text;
using Application.Identity.Tokens.Claims;
using Application.Inventory.Abstractions;
using Application.Inventory.Commands;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Domain.Inventory.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Inventory;

public sealed class InventoryRefillTaskCommandHandlerTests
{
    [Fact]
    public async Task Request_rejects_non_positive_requested_quantity()
    {
        var store = Substitute.For<IInventoryStore>();
        var handler = new RequestInventoryRefillTaskCommandHandler(store);

        var result = await handler.HandleAsync(new RequestInventoryRefillTaskCommand(
            Guid.NewGuid(), Guid.NewGuid(), 0, null, null, null, "request-1", SystemAdmin()));

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("greater than zero", result.Message);
    }

    [Fact]
    public async Task Request_rechecks_idempotency_key_after_inventory_lock()
    {
        var kioskId = Guid.NewGuid();
        var inventoryId = Guid.NewGuid();
        var requestKey = "request-1";
        const decimal requestedQuantity = 10;
        var store = Substitute.For<IInventoryStore>();
        var handler = new RequestInventoryRefillTaskCommandHandler(store);
        var balance = new KioskIngredientInventory
        {
            Id = inventoryId,
            KioskId = kioskId,
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid()
        };
        var replay = new InventoryRefillTask
        {
            Id = Guid.NewGuid(),
            KioskId = kioskId,
            KioskIngredientInventoryId = inventoryId,
            RequestFingerprint = Fingerprint("request", inventoryId, requestedQuantity),
            RequestIdempotencyKey = requestKey
        };

        store.GetInventoryRefillTaskByRequestKeyAsync(kioskId, requestKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InventoryRefillTask?>(null), Task.FromResult<InventoryRefillTask?>(replay));
        store.GetKioskIngredientInventoryAsync(inventoryId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<KioskIngredientInventory?>(balance));
        store.AcquireKioskIngredientInventoryMutationLockAsync(inventoryId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<InventoryRefillTaskResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<ApiResult<InventoryRefillTaskResult>>>>(0)(CancellationToken.None));

        var result = await handler.HandleAsync(new RequestInventoryRefillTaskCommand(
            kioskId, inventoryId, requestedQuantity, null, null, null, requestKey, SystemAdmin()));

        Assert.True(result.Succeeded);
        Assert.Equal(replay.Id, result.Data!.Id);
        await store.DidNotReceive().AddInventoryRefillTaskAsync(Arg.Any<InventoryRefillTask>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_returns_not_found_when_task_disappears_after_lock()
    {
        var kioskId = Guid.NewGuid();
        var task = new InventoryRefillTask
        {
            Id = Guid.NewGuid(),
            KioskId = kioskId,
            KioskIngredientInventoryId = Guid.NewGuid()
        };
        var store = Substitute.For<IInventoryStore>();
        var handler = new CompleteInventoryRefillTaskCommandHandler(store);
        store.GetInventoryRefillTaskAsync(task.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InventoryRefillTask?>(task), Task.FromResult<InventoryRefillTask?>(null));
        store.AcquireKioskIngredientInventoryMutationLockAsync(task.KioskIngredientInventoryId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.AcquireInventoryRefillTaskMutationLockAsync(task.Id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<InventoryRefillTaskResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<ApiResult<InventoryRefillTaskResult>>>>(0)(CancellationToken.None));

        var result = await handler.HandleAsync(new CompleteInventoryRefillTaskCommand(
            kioskId, task.Id, 10, null, null, null, null, "complete-1", SystemAdmin()));

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
    }

    private static CurrentUserContext SystemAdmin() => new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true };

    private static string Fingerprint(string operation, Guid inventoryId, decimal quantity)
    {
        var raw = $"{operation}|{inventoryId:N}|{quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}|||";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
