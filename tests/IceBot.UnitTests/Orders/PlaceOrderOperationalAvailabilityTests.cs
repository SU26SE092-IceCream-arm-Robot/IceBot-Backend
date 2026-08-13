using Application.Devices.Telemetry;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Services;
using Application.SalesCatalog.Availability;
using Domain.Orders.Entities;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Orders;

public sealed class PlaceOrderOperationalAvailabilityTests
{
    [Fact]
    public async Task AppendAsync_RejectsPausedMenuItemBeforeCreatingOrderItem()
    {
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid()
        };
        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            DisplayName = "Vanilla soft serve"
        };
        var orderStore = Substitute.For<IOrderStore>();
        orderStore.GetMenuItemForKioskAsync(
                menuItem.Id, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id, Arg.Any<CancellationToken>())
            .Returns(menuItem);
        var availability = Substitute.For<IMenuItemOperationalAvailabilityReader>();
        availability.IsPausedAsync(kiosk.Id, menuItem.Id, Arg.Any<CancellationToken>()).Returns(true);
        var appender = new PlaceOrderItemAppender(
            orderStore,
            availability,
            Options.Create(new EdgeTelemetryIngestionOptions()));

        var failure = await appender.AppendAsync(
            new Order(),
            kiosk,
            new PlaceOrderItemRequest { MenuItemId = menuItem.Id, Quantity = 1 },
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.NotNull(failure);
        Assert.Equal(409, failure.StatusCode);
        Assert.Contains("paused", failure.Message, StringComparison.OrdinalIgnoreCase);
        await orderStore.DidNotReceive().ListMenuItemProductOptionsAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
