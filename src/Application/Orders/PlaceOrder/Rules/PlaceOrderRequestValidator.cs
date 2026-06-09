using Application.Orders.PlaceOrder.Requests;
using System;
using System.Linq;

namespace Application.Orders.PlaceOrder.Rules;

internal static class PlaceOrderRequestValidator
{
    public static string? Validate(PlaceOrderRequest request)
    {
        if (request.KioskId == Guid.Empty)
        {
            return "Kiosk is required.";
        }

        if (request.Items.Count == 0)
        {
            return "Order must contain at least one item.";
        }

        if (request.Items.Any(item => item.MenuItemId == Guid.Empty))
        {
            return "Menu item is required for every order item.";
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return "Order item quantity must be greater than zero.";
        }

        var duplicateClientLineId = request.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.ClientLineId))
            .GroupBy(item => item.ClientLineId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        return duplicateClientLineId ? "Duplicate client line id in order items." : null;
    }
}
