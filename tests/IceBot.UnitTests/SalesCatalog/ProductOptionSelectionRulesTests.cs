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

        Assert.NotNull(error);
        Assert.Equal(ProductOptionSelectionFailureCode.MinimumSelectionNotMet, error.Code);
        Assert.Equal("Option group 'Toppings' requires at least 1 selection(s).", error.Message);
    }

    [Fact]
    public void Validate_RejectsOptionOutsideMenuItemAvailability()
    {
        var error = ProductOptionSelectionRules.Validate([], [Guid.NewGuid()]);

        Assert.NotNull(error);
        Assert.Equal(ProductOptionSelectionFailureCode.OptionUnavailable, error.Code);
        Assert.Equal("One or more selected product options are unavailable for this menu item.", error.Message);
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

    [Fact]
    public void IsSatisfiable_RejectsRequiredGroupWhoseOptionUsesInactiveIngredient()
    {
        var option = CreateOption(isRequired: true, minSelections: 1) with
        {
            AreIngredientRequirementsActive = false
        };

        Assert.False(ProductOptionSelectionRules.IsSatisfiable([option]));
        var error = ProductOptionSelectionRules.Validate([option], [option.ProductOptionId]);
        Assert.NotNull(error);
        Assert.Equal(ProductOptionSelectionFailureCode.OptionUnavailable, error.Code);
    }

    [Fact]
    public void IsSatisfiable_RejectsRequiredGroupWithoutAnyMenuMembership()
    {
        var group = CreateGroup(isRequired: true, minSelections: 1);

        Assert.False(ProductOptionSelectionRules.IsSatisfiable([group], []));
    }

    [Fact]
    public void Validate_RejectsMissingSelectionWhenRequiredGroupHasNoMenuMembership()
    {
        var group = CreateGroup(isRequired: true, minSelections: 1);

        var error = ProductOptionSelectionRules.Validate([group], [], []);

        Assert.NotNull(error);
        Assert.Equal(ProductOptionSelectionFailureCode.MinimumSelectionNotMet, error.Code);
        Assert.Equal("Option group 'Toppings' requires at least 1 selection(s).", error.Message);
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
            ProductOptionExecutionImpact.ProductionAffecting,
            true,
            true,
            false,
            1);
    }

    private static MenuItemOptionGroupReadModel CreateGroup(bool isRequired, int minSelections) =>
        new(
            Guid.NewGuid(),
            1,
            "TOPPING",
            "Toppings",
            OptionSelectionType.Single,
            minSelections,
            1,
            isRequired);
}
