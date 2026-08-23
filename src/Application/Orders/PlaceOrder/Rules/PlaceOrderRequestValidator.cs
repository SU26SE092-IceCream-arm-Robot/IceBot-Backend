using Application.Orders.PlaceOrder.Requests;
using Application.ClientDevices;
using Microsoft.Extensions.Options;

namespace Application.Orders.PlaceOrder.Rules;

internal static class PlaceOrderRequestValidator
{
    public static Dictionary<string, List<string>>? Validate(PlaceOrderRequest request, ClientDeviceRuntimeOptions limits)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(limits);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        AddLengthError(errors, "clientOrderId", request.ClientOrderId, limits.MaxClientOrderIdLength, "Client order id");
        AddLengthError(errors, "customerName", request.CustomerName, limits.MaxCustomerNameLength, "Customer name");
        AddLengthError(errors, "customerPhoneNumber", request.CustomerPhoneNumber, limits.MaxCustomerPhoneNumberLength, "Customer phone number");
        AddLengthError(errors, "notes", request.Notes, limits.MaxNotesLength, "Order notes");

        if (request.ClientTotalAmount.HasValue && request.ClientTotalAmount.Value <= 0)
            AddError(errors, "clientTotalAmount", "Client total amount must be greater than zero when provided.");

        if (request.Items is null || request.Items.Count == 0)
        {
            AddError(errors, "items", "Order must contain at least one item.");
            return errors;
        }

        if (request.Items.Count > limits.MaxOrderLines)
        {
            AddError(errors, "items", $"Order cannot contain more than {limits.MaxOrderLines} items.");
            return errors;
        }

        long totalUnits = 0;
        var normalizedClientLineIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var itemIndex = 0; itemIndex < request.Items.Count; itemIndex++)
        {
            var item = request.Items[itemIndex];
            var itemPath = $"items[{itemIndex}]";
            if (item is null)
            {
                AddError(errors, itemPath, "Order item is required.");
                continue;
            }

            if (item.MenuItemId == Guid.Empty)
                AddError(errors, $"{itemPath}.menuItemId", "Menu item is required.");

            if (item.Quantity <= 0 || item.Quantity > limits.MaxQuantityPerLine)
            {
                AddError(errors, $"{itemPath}.quantity", $"Order item quantity must be between 1 and {limits.MaxQuantityPerLine}.");
            }
            else
            {
                totalUnits += item.Quantity;
            }

            AddLengthError(errors, $"{itemPath}.clientLineId", item.ClientLineId, limits.MaxClientLineIdLength, "Client line id");
            if (!string.IsNullOrWhiteSpace(item.ClientLineId))
            {
                var normalizedClientLineId = item.ClientLineId.Trim();
                if (!normalizedClientLineIds.TryAdd(normalizedClientLineId, itemIndex))
                {
                    AddError(errors, $"{itemPath}.clientLineId", "Client line id must be unique within an order.");
                }
            }

            if (item.SelectedOptions is null)
            {
                AddError(errors, $"{itemPath}.selectedOptions", "Selected options must be provided.");
                continue;
            }

            if (item.SelectedOptions.Count > limits.MaxSelectedOptionsPerLine)
            {
                AddError(errors, $"{itemPath}.selectedOptions", $"An order item cannot contain more than {limits.MaxSelectedOptionsPerLine} selected options.");
                continue;
            }

            var selectedOptionIds = new HashSet<Guid>();
            for (var optionIndex = 0; optionIndex < item.SelectedOptions.Count; optionIndex++)
            {
                var option = item.SelectedOptions[optionIndex];
                var optionPath = $"{itemPath}.selectedOptions[{optionIndex}].productOptionId";
                if (option is null || option.ProductOptionId == Guid.Empty)
                {
                    AddError(errors, optionPath, "Selected product option is required.");
                    continue;
                }

                if (!selectedOptionIds.Add(option.ProductOptionId))
                    AddError(errors, optionPath, "Selected product options must be unique within an order item.");
            }
        }

        if (totalUnits > limits.MaxTotalUnits)
            AddError(errors, "items", $"Order cannot contain more than {limits.MaxTotalUnits} units.");

        return errors.Count == 0 ? null : errors;
    }

    private static void AddLengthError(
        Dictionary<string, List<string>> errors,
        string field,
        string? value,
        int maximum,
        string displayName)
    {
        if (ExceedsLength(value, maximum))
            AddError(errors, field, $"{displayName} cannot exceed {maximum} characters.");
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors = [];
            errors[field] = fieldErrors;
        }

        fieldErrors.Add(message);
    }

    private static bool ExceedsLength(string? value, int maximum) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum;
}
