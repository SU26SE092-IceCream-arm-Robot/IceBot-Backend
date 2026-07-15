using Application.Catalog.Products.Rules;
using Domain.Catalog.Enums;

namespace IceBot.UnitTests.Catalog;

public sealed class ProductOptionRequestValidatorTests
{
    [Fact]
    public void RequiredGroup_RejectsZeroMinimum()
    {
        var error = ProductOptionRequestValidator.ValidateGroup("TOPPING", "Topping", OptionSelectionType.Multiple, 0, 3, true);
        Assert.Equal("Required option groups must require at least one selection.", error);
    }

    [Fact]
    public void SingleGroup_RequiresMaximumOne()
    {
        var error = ProductOptionRequestValidator.ValidateGroup("SIZE", "Size", OptionSelectionType.Single, 1, 2, true);
        Assert.Equal("Single-select option groups must have maximum selections equal to one.", error);
    }

    [Fact]
    public void Option_RejectsNegativePriceDelta()
    {
        var error = ProductOptionRequestValidator.ValidateOption(
            "OREO", "Oreo", -1, ProductOptionExecutionImpact.CommercialOnly);
        Assert.Equal("Product option price delta cannot be negative.", error);
    }

    [Fact]
    public void Option_RejectsUnknownExecutionImpact()
    {
        var error = ProductOptionRequestValidator.ValidateOption(
            "OREO", "Oreo", 0, (ProductOptionExecutionImpact)99);

        Assert.Equal("Product option execution impact is invalid.", error);
    }
}
