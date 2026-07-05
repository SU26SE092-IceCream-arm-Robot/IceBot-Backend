using Application.SalesCatalog.ReadModels;
using Domain.Catalog.Enums;

namespace Application.SalesCatalog.Rules;

public static class ProductOptionSelectionRules
{
    public static string? Validate(
        IReadOnlyCollection<MenuItemProductOptionReadModel> options,
        IReadOnlyCollection<Guid> selectedOptionIds)
    {
        if (selectedOptionIds.Count != selectedOptionIds.Distinct().Count())
        {
            return "Selected product options must be unique.";
        }

        var selected = options
            .Where(option => selectedOptionIds.Contains(option.ProductOptionId))
            .ToArray();

        if (selected.Length != selectedOptionIds.Count || selected.Any(option => !option.IsAvailable))
        {
            return "One or more selected product options are unavailable for this menu item.";
        }

        foreach (var group in options.GroupBy(option => option.OptionGroupId))
        {
            var definition = group.First();
            var count = selected.Count(option => option.OptionGroupId == group.Key);
            var minimum = definition.IsRequired ? Math.Max(1, definition.MinSelections) : definition.MinSelections;
            var maximum = definition.SelectionType == OptionSelectionType.Single
                ? 1
                : definition.MaxSelections;

            if (count < minimum)
            {
                return $"Option group '{definition.OptionGroupName}' requires at least {minimum} selection(s).";
            }

            if (maximum > 0 && count > maximum)
            {
                return $"Option group '{definition.OptionGroupName}' allows at most {maximum} selection(s).";
            }
        }

        return null;
    }

    public static bool IsSatisfiable(IReadOnlyCollection<MenuItemProductOptionReadModel> options)
    {
        return options.GroupBy(option => option.OptionGroupId).All(group =>
        {
            var definition = group.First();
            var minimum = definition.IsRequired ? Math.Max(1, definition.MinSelections) : definition.MinSelections;
            var maximum = definition.SelectionType == OptionSelectionType.Single ? 1 : definition.MaxSelections;
            var availableCount = group.Count(option => option.IsAvailable);
            return availableCount >= minimum && (maximum <= 0 || minimum <= maximum);
        });
    }
}
