using Application.SalesCatalog.ReadModels;
using Domain.Catalog.Enums;

namespace Application.SalesCatalog.Rules;

public enum ProductOptionSelectionFailureCode
{
    DuplicateSelection,
    OptionUnavailable,
    MinimumSelectionNotMet,
    MaximumSelectionExceeded
}

public sealed record ProductOptionSelectionFailure(
    ProductOptionSelectionFailureCode Code,
    string Message);

public static class ProductOptionSelectionRules
{
    public static ProductOptionSelectionFailure? Validate(
        IReadOnlyCollection<MenuItemProductOptionReadModel> options,
        IReadOnlyCollection<Guid> selectedOptionIds)
    {
        return Validate(BuildGroupDefinitions(options), options, selectedOptionIds);
    }

    public static ProductOptionSelectionFailure? Validate(
        IReadOnlyCollection<MenuItemOptionGroupReadModel> groups,
        IReadOnlyCollection<MenuItemProductOptionReadModel> options,
        IReadOnlyCollection<Guid> selectedOptionIds)
    {
        if (selectedOptionIds.Count != selectedOptionIds.Distinct().Count())
        {
            return new(ProductOptionSelectionFailureCode.DuplicateSelection, "Selected product options must be unique.");
        }

        var selected = options
            .Where(option => selectedOptionIds.Contains(option.ProductOptionId))
            .ToArray();

        if (selected.Length != selectedOptionIds.Count || selected.Any(option => !IsSelectable(option)))
        {
            return new(
                ProductOptionSelectionFailureCode.OptionUnavailable,
                "One or more selected product options are unavailable for this menu item.");
        }

        foreach (var definition in groups)
        {
            var count = selected.Count(option => option.OptionGroupId == definition.OptionGroupId);
            var minimum = definition.IsRequired ? Math.Max(1, definition.MinSelections) : definition.MinSelections;
            var maximum = definition.SelectionType == OptionSelectionType.Single
                ? 1
                : definition.MaxSelections;

            if (count < minimum)
            {
                return new(
                    ProductOptionSelectionFailureCode.MinimumSelectionNotMet,
                    $"Option group '{definition.OptionGroupName}' requires at least {minimum} selection(s).");
            }

            if (maximum > 0 && count > maximum)
            {
                return new(
                    ProductOptionSelectionFailureCode.MaximumSelectionExceeded,
                    $"Option group '{definition.OptionGroupName}' allows at most {maximum} selection(s).");
            }
        }

        return null;
    }

    public static bool IsSatisfiable(IReadOnlyCollection<MenuItemProductOptionReadModel> options)
    {
        return IsSatisfiable(BuildGroupDefinitions(options), options);
    }

    public static bool IsSatisfiable(
        IReadOnlyCollection<MenuItemOptionGroupReadModel> groups,
        IReadOnlyCollection<MenuItemProductOptionReadModel> options)
    {
        return groups.All(definition =>
        {
            var minimum = definition.IsRequired ? Math.Max(1, definition.MinSelections) : definition.MinSelections;
            var maximum = definition.SelectionType == OptionSelectionType.Single ? 1 : definition.MaxSelections;
            var availableCount = options.Count(option =>
                option.OptionGroupId == definition.OptionGroupId && IsSelectable(option));
            return availableCount >= minimum && (maximum <= 0 || minimum <= maximum);
        });
    }

    private static MenuItemOptionGroupReadModel[] BuildGroupDefinitions(
        IReadOnlyCollection<MenuItemProductOptionReadModel> options) =>
        options.GroupBy(option => option.OptionGroupId)
            .Select(group =>
            {
                var option = group.First();
                return new MenuItemOptionGroupReadModel(
                    option.MenuItemId,
                    option.OptionGroupId,
                    option.OptionGroupCode,
                    option.OptionGroupName,
                    option.SelectionType,
                    option.MinSelections,
                    option.MaxSelections,
                    option.IsRequired);
            })
            .ToArray();

    public static bool IsSelectable(MenuItemProductOptionReadModel option) =>
        option.IsAvailable && option.AreIngredientRequirementsActive;
}
