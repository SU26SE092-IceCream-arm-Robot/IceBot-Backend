using Application.Orders.PlaceOrder.Results;
using Domain.Orders.Entities;
using Domain.ProductionExecution.Projections;

namespace Application.Orders.PlaceOrder.Mapping;

internal static class OrderResultMapper
{
    public static OrderResult ToResult(Order order, OrderExecutionRecord? executionRecord = null)
    {
        var customerStatusInfo = Application.Orders.Support.OrderStatusProjector.ProjectFromOrderAndExecution(order, executionRecord);

        return new OrderResult
        {
            Id = order.Id,
            KioskId = order.KioskId,
            OrderNumber = order.OrderNumber,
            ClientOrderId = order.ClientOrderId,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            Currency = order.Currency,
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount,
            PlacedAt = order.PlacedAt,
            PaymentDeadlineAt = order.PaymentDeadlineAt,
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
                    MenuItemCode = item.MenuItemCodeSnapshot,
                    MenuItemName = item.MenuItemNameSnapshot,
                    ProductCode = item.ProductCodeSnapshot,
                    ProductName = item.ProductNameSnapshot,
                    ProductVariantCode = item.ProductVariantCodeSnapshot,
                    ProductVariantName = item.ProductVariantNameSnapshot,
                    RecipeVersion = item.RecipeVersionSnapshot,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalAmount = item.TotalAmount,
                    SelectedOptions = item.Options
                        .OrderBy(option => option.OptionGroupCodeSnapshot)
                        .ThenBy(option => option.CodeSnapshot)
                        .Select(option => new OrderItemOptionResult
                        {
                            ProductOptionId = option.ProductOptionId,
                            OptionGroupCode = option.OptionGroupCodeSnapshot,
                            Code = option.CodeSnapshot,
                            Name = option.NameSnapshot,
                            PriceDelta = option.UnitPriceDelta
                        }).ToList(),
                    Status = item.Status
                })
                .ToList()
        };
    }
}
