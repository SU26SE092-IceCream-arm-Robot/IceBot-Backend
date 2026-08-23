using Application.Orders.PlaceOrder.Requests;
using Application.ClientDevices;
using Microsoft.Extensions.Options;

namespace Application.Orders.PlaceOrder.Rules;

internal static class PlaceOrderRequestValidator
{
    public static string? Validate(PlaceOrderRequest request, ClientDeviceRuntimeOptions limits)
    {
        if (ExceedsLength(request.ClientOrderId, limits.MaxClientOrderIdLength))
            return $"Client order id cannot exceed {limits.MaxClientOrderIdLength} characters.";
        if (ExceedsLength(request.CustomerName, limits.MaxCustomerNameLength))
            return $"Customer name cannot exceed {limits.MaxCustomerNameLength} characters.";
        if (ExceedsLength(request.CustomerPhoneNumber, limits.MaxCustomerPhoneNumberLength))
            return $"Customer phone number cannot exceed {limits.MaxCustomerPhoneNumberLength} characters.";
        if (ExceedsLength(request.Notes, limits.MaxNotesLength))
            return $"Order notes cannot exceed {limits.MaxNotesLength} characters.";

        if (request.Items.Count == 0)
        {
            return "Order must contain at least one item.";
        }

        if (request.Items.Count > limits.MaxOrderLines)
        {
            return $"Order cannot contain more than {limits.MaxOrderLines} items.";
        }

        if (request.Items.Any(item => item.MenuItemId == Guid.Empty))
        {
            return "Menu item is required for every order item.";
        }

        if (request.Items.Any(item => item.Quantity <= 0 || item.Quantity > limits.MaxQuantityPerLine))
        {
            return $"Order item quantity must be between 1 and {limits.MaxQuantityPerLine}.";
        }

        if (request.Items.Sum(item => item.Quantity) > limits.MaxTotalUnits)
        {
            return $"Order cannot contain more than {limits.MaxTotalUnits} units.";
        }

        if (request.Items.Any(item => item.SelectedOptions.Count > limits.MaxSelectedOptionsPerLine))
        {
            return $"An order item cannot contain more than {limits.MaxSelectedOptionsPerLine} selected options.";
        }

        if (request.Items.Any(item => item.SelectedOptions.Any(option => option.ProductOptionId == Guid.Empty)))
        {
            return "Every selected product option must have a valid id.";
        }

        if (request.Items.Any(item => item.SelectedOptions.Select(option => option.ProductOptionId).Distinct().Count() != item.SelectedOptions.Count))
        {
            return "Selected product options must be unique within an order item.";
        }

        if (request.Items.Any(item => ExceedsLength(item.ClientLineId, limits.MaxClientLineIdLength)))
            return $"Client line id cannot exceed {limits.MaxClientLineIdLength} characters.";

        var duplicateClientLineId = request.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.ClientLineId))
            .GroupBy(item => item.ClientLineId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        return duplicateClientLineId ? "Duplicate client line id in order items." : null;
    }

    private static bool ExceedsLength(string? value, int maximum) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum;
}
