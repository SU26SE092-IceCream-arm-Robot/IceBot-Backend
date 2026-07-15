using Application.Orders.Management.Results;
using Application.Orders.Support;
using Domain.Orders.Entities;

namespace Application.Orders.Management.Mapping;

internal static class ManagementOrderResultMapper
{
    public static ManagementOrderListItemResult ToListItem(Order order)
    {
        var projection = OrderStatusProjector.ProjectFromOrder(order);
        return new ManagementOrderListItemResult
        {
            Id = order.Id,
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            KioskId = order.KioskId,
            OrderNumber = order.OrderNumber,
            ClientOrderId = order.ClientOrderId,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            Currency = order.Currency,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount,
            CustomerName = order.CustomerName,
            CustomerPhoneNumber = order.CustomerPhoneNumber,
            PlacedAt = order.PlacedAt,
            PaidAt = order.PaidAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt,
            CustomerStatus = projection.CustomerStatus,
            CanRetryPayment = projection.CanRetryPayment,
            RequiresStaffSupport = projection.RequiresStaffSupport
        };
    }

    public static ManagementOrderDetailResult ToDetail(Order order)
    {
        var projection = OrderStatusProjector.ProjectFromOrder(order);
        return new ManagementOrderDetailResult
        {
            Id = order.Id,
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            KioskId = order.KioskId,
            OrderNumber = order.OrderNumber,
            ClientOrderId = order.ClientOrderId,
            Channel = order.Channel,
            ExternalChannel = order.ExternalChannel,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            Currency = order.Currency,
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount,
            CustomerName = order.CustomerName,
            CustomerPhoneNumber = order.CustomerPhoneNumber,
            Notes = order.Notes,
            PlacedAt = order.PlacedAt,
            PaidAt = order.PaidAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt,
            CustomerStatus = projection.CustomerStatus,
            CustomerStatusMessage = projection.CustomerStatusMessage,
            CanRetryPayment = projection.CanRetryPayment,
            RequiresStaffSupport = projection.RequiresStaffSupport,
            Items = order.OrderItems
                .OrderBy(item => item.CreatedAt)
                .Select(item => new ManagementOrderItemResult
                {
                    Id = item.Id,
                    MenuItemId = item.MenuItemId,
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    RecipeId = item.RecipeId,
                    ClientLineId = item.ClientLineId,
                    MenuItemCode = item.MenuItemCodeSnapshot,
                    MenuItemName = item.MenuItemNameSnapshot,
                    ProductCode = item.ProductCodeSnapshot,
                    ProductName = item.ProductNameSnapshot,
                    ProductVariantCode = item.ProductVariantCodeSnapshot,
                    ProductVariantName = item.ProductVariantNameSnapshot,
                    RecipeVersion = item.RecipeVersionSnapshot,
                    FulfillmentType = item.FulfillmentType,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalAmount = item.TotalAmount,
                    Status = item.Status,
                    SelectedOptions = item.Options
                        .OrderBy(option => option.OptionGroupCodeSnapshot)
                        .ThenBy(option => option.CodeSnapshot)
                        .Select(option => new ManagementOrderItemOptionResult
                        {
                            ProductOptionId = option.ProductOptionId,
                            OptionGroupCode = option.OptionGroupCodeSnapshot,
                            Code = option.CodeSnapshot,
                            Name = option.NameSnapshot,
                            PriceDelta = option.UnitPriceDelta
                        })
                        .ToArray()
                })
                .ToArray()
        };
    }
}
