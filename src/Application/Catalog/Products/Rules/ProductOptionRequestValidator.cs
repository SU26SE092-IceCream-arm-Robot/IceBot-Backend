using Domain.Catalog.Enums;

namespace Application.Catalog.Products.Rules;

public static class ProductOptionRequestValidator
{
    public static string? ValidateGroup(
        string? code,
        string? name,
        OptionSelectionType selectionType,
        int minSelections,
        int maxSelections,
        bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 100) return "Option group code is required and must not exceed 100 characters.";
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return "Option group name is required and must not exceed 200 characters.";
        if (!Enum.IsDefined(selectionType)) return "Option group selection type is invalid.";
        if (minSelections < 0) return "Minimum selections cannot be negative.";
        if (maxSelections <= 0) return "Maximum selections must be greater than zero.";
        if (selectionType == OptionSelectionType.Single && maxSelections != 1) return "Single-select option groups must have maximum selections equal to one.";
        if (minSelections > maxSelections) return "Minimum selections cannot exceed maximum selections.";
        if (isRequired && minSelections == 0) return "Required option groups must require at least one selection.";
        return null;
    }

    public static string? ValidateOption(string? code, string? name, decimal priceDelta)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 100) return "Product option code is required and must not exceed 100 characters.";
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return "Product option name is required and must not exceed 200 characters.";
        return priceDelta < 0 ? "Product option price delta cannot be negative." : null;
    }
}
