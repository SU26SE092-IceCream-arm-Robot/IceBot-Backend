using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.Rules;
using Domain.Catalog.Enums;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class ProductOptionSelectionRulesTests
{
    [Fact]
    public void Validate_RejectsMissingRequiredSelection()
    {
        var option = CreateOption(isRequired: true, minSelections: 1);

        var error = ProductOptionSelectionRules.Validate([option], []);

        Assert.Equal("Option group 'Toppings' requires at least 1 selection(s).", error);
    }

    [Fact]
    public void Validate_RejectsOptionOutsideMenuItemAvailability()
    {
        var error = ProductOptionSelectionRules.Validate([], [Guid.NewGuid()]);

        Assert.Equal("One or more selected product options are unavailable for this menu item.", error);
    }

    [Fact]
    public void Validate_AcceptsOneAvailableOptionInSingleGroup()
    {
        var option = CreateOption(isRequired: true, minSelections: 1);

        var error = ProductOptionSelectionRules.Validate([option], [option.ProductOptionId]);

        Assert.Null(error);
    }

    [Fact]
    public void IsSatisfiable_RejectsRequiredGroupWithoutAvailableOptions()
    {
        var option = CreateOption(isRequired: true, minSelections: 1) with { IsAvailable = false };

        Assert.False(ProductOptionSelectionRules.IsSatisfiable([option]));
    }

    private static MenuItemProductOptionReadModel CreateOption(bool isRequired, int minSelections)
    {
        return new MenuItemProductOptionReadModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "TOPPING",
            "Toppings",
            OptionSelectionType.Single,
            minSelections,
            1,
            isRequired,
            "OREO",
            "Oreo",
            null,
            5000,
            true,
            false,
            1);
    }
}
