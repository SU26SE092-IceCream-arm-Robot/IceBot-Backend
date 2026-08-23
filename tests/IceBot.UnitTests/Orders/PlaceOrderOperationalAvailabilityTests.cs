using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Services;
using Application.SalesCatalog.Admission;
using Application.SalesCatalog.Admission.Abstractions;
using Domain.Catalog.Enums;
using Domain.Catalog.Entities;
using Domain.Orders.Entities;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
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
            DisplayName = "Vanilla soft serve",
            ProductVariant = new ProductVariant
            {
                FulfillmentType = FulfillmentType.MachineProduced
            }
        };
        var orderStore = Substitute.For<IOrderStore>();
        orderStore.GetMenuItemForKioskAsync(
                menuItem.Id, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id, Arg.Any<CancellationToken>())
            .Returns(menuItem);
        orderStore.ListMenuItemProductOptionsAsync(menuItem.Id, Arg.Any<CancellationToken>())
            .Returns([]);
        orderStore.ListMenuItemOptionGroupsAsync(menuItem.Id, Arg.Any<CancellationToken>())
            .Returns([]);
        orderStore.ListProductOptionIngredientRequirementsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var admission = Substitute.For<IMenuItemOperationalAdmissionEvaluator>();
        admission.EvaluateAsync(
                kiosk,
                menuItem.Id,
                1,
                Arg.Any<IReadOnlyCollection<Application.Inventory.Abstractions.InventoryIngredientRequirementInput>?>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(new MenuItemOperationalDecision(
                menuItem.Id,
                false,
                [new SalesAdmissionBlocker(
                    SalesAdmissionBlockerCode.MenuItemPaused,
                    SalesAdmissionBlockerScope.MenuItem)],
                [],
                new HashSet<string>()));
        var appender = new PlaceOrderItemAppender(orderStore, admission);

        var failure = await appender.AppendAsync(
            new Order(),
            kiosk,
            new PlaceOrderItemRequest { MenuItemId = menuItem.Id, Quantity = 1 },
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.NotNull(failure);
        Assert.Equal(409, failure.StatusCode);
        Assert.Equal("SALES.MENU_ITEM_PAUSED", failure.BusinessError!.Code);
        Assert.Contains("paused", failure.Message, StringComparison.OrdinalIgnoreCase);
    }
}
