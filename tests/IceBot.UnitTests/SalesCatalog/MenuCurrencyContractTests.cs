using Application.Identity.Tokens.Claims;
using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Commands;
using Application.SalesCatalog.Menus.Requests;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Tenants.Enums;
using NSubstitute;
using Application.Shared.Concurrency;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class MenuCurrencyContractTests
{
    [Fact]
    public async Task UpdateMenuCurrency_RejectsMenuWithItems()
    {
        var organizationId = Guid.NewGuid();
        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = "MAIN",
            Name = "Main",
            Currency = "VND",
            ScopeType = TenantScopeType.Organization,
            MenuItems =
            [
                new MenuItem { Id = Guid.NewGuid(), Code = "A", DisplayName = "A", Currency = "VND" },
                new MenuItem { Id = Guid.NewGuid(), Code = "B", DisplayName = "B", Currency = "VND" }
            ]
        };
        var store = Substitute.For<IMenuStore>();
        store.GetMenuByIdAsync(menu.Id, false, Arg.Any<CancellationToken>()).Returns(menu);
        store.MenuCodeExistsAsync(
                organizationId, null, null, "MAIN", menu.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await new UpdateMenuCommandHandler(
            store, InlineTechnicalResourceMutationCoordinator.Instance).HandleAsync(new UpdateMenuCommand
            {
                Scope = new MenuManagementCommandScope(
                new CurrentUserContext { IsSystemAdmin = true }, organizationId),
                MenuId = menu.Id,
                Request = new UpdateMenuRequest { Currency = "USD" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("VND", menu.Currency);
        Assert.All(menu.MenuItems, item => Assert.Equal("VND", item.Currency));
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateMenuItem_RejectsMachineProducedItemWithoutRecipe()
    {
        var organizationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = "MAIN",
            Name = "Main",
            Currency = "VND",
            ScopeType = TenantScopeType.Organization
        };
        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            MenuId = menu.Id,
            ProductId = productId,
            ProductVariantId = variantId,
            Code = "ICE_CREAM",
            DisplayName = "Ice cream",
            Status = MenuItemStatus.Draft
        };
        var store = Substitute.For<IMenuStore>();
        store.GetMenuByIdAsync(menu.Id, true, Arg.Any<CancellationToken>()).Returns(menu);
        store.GetMenuItemByIdAsync(menu.Id, item.Id, false, Arg.Any<CancellationToken>()).Returns(item);
        store.GetProductByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(new Product
        {
            Id = productId,
            OrganizationId = organizationId,
            Code = "ICE_CREAM",
            Name = "Ice cream",
            Currency = "VND"
        });
        store.GetProductVariantByIdAsync(variantId, Arg.Any<CancellationToken>()).Returns(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Code = "DEFAULT",
            Name = "Default",
            FulfillmentType = FulfillmentType.MachineProduced
        });

        var result = await new SetMenuItemStatusCommandHandler(store).HandleAsync(new SetMenuItemStatusCommand
        {
            Scope = new MenuManagementCommandScope(
                new CurrentUserContext { IsSystemAdmin = true }, organizationId),
            MenuId = menu.Id,
            MenuItemId = item.Id,
            Status = MenuItemStatus.Active
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(MenuItemStatus.Draft, item.Status);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
