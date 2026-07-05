using Application.Identity.Tokens.Claims;
using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Commands;
using Application.SalesCatalog.Menus.Requests;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Enums;
using NSubstitute;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class MenuCurrencyContractTests
{
    [Fact]
    public async Task UpdateMenuCurrency_PropagatesToAllItems()
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

        var result = await new UpdateMenuCommandHandler(store).HandleAsync(new UpdateMenuCommand
        {
            Scope = new MenuManagementCommandScope(
                new CurrentUserContext { IsSystemAdmin = true }, organizationId),
            MenuId = menu.Id,
            Request = new UpdateMenuRequest { Currency = "USD" }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal("USD", menu.Currency);
        Assert.All(menu.MenuItems, item => Assert.Equal("USD", item.Currency));
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
