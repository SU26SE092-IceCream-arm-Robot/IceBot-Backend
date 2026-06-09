using Application.Orders.PlaceOrder.Results;
using Domain.Orders.Entities;
using System.Linq;

namespace Application.Orders.PlaceOrder.Mapping;

internal static class OrderResultMapper
{
    public static OrderResult ToResult(Order order)
    {
        var customerStatusInfo = Application.Shared.Utils.OrderStatusProjector.ProjectFromOrder(order);

        return new OrderResult
        {
            Id = order.Id,
            KioskId = order.KioskId,
            StoreId = order.StoreId,
            OrganizationId = order.OrganizationId,
            OrderNumber = order.OrderNumber,
            ClientOrderId = order.ClientOrderId,
            RuntimeSnapshotId = order.RuntimeSnapshotId,
            RuntimeSnapshotGeneratedAt = order.RuntimeSnapshotGeneratedAt,
            Channel = order.Channel,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            Currency = order.Currency,
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount,
            PlacedAt = order.PlacedAt,
            PaidAt = order.PaidAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt,
            CustomerStatus = customerStatusInfo.CustomerStatus,
            CustomerStatusMessage = customerStatusInfo.CustomerStatusMessage,
            CanRetryPayment = customerStatusInfo.CanRetryPayment,
            RequiresStaffSupport = customerStatusInfo.RequiresStaffSupport,
            Items = order.OrderItems
                .OrderBy(item => item.CreatedAt)
                .Select(item => new OrderItemResult
                {
                    Id = item.Id,
                    MenuItemId = item.MenuItemId,
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    RecipeId = item.RecipeId,
                    ClientLineId = item.ClientLineId,
                    MenuItemCodeSnapshot = item.MenuItemCodeSnapshot,
                    MenuItemNameSnapshot = item.MenuItemNameSnapshot,
                    ProductCodeSnapshot = item.ProductCodeSnapshot,
                    ProductNameSnapshot = item.ProductNameSnapshot,
                    ProductVariantCodeSnapshot = item.ProductVariantCodeSnapshot,
                    ProductVariantNameSnapshot = item.ProductVariantNameSnapshot,
                    RecipeVersionSnapshot = item.RecipeVersionSnapshot,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalAmount = item.TotalAmount,
                    Status = item.Status
                })
                .ToList()
        };
    }
}
