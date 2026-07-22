using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.Shared.Wrappers;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Sync.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class InitialDispatchEscalationTests
{
    [Fact]
    public async Task PaidOrderWithoutInitialCommandPastSla_BecomesFulfillmentIssue()
    {
        var paidAt = DateTimeOffset.UtcNow.AddMinutes(-20);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            KioskId = Guid.NewGuid(),
            OrderNumber = "ORDER-DISPATCH-SLA"
        };
        order.SetCurrency("VND");
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "ITEM", "Item", "PRODUCT", "Product", "VARIANT", "Variant", null,
            FulfillmentType.MachineProduced, 1, 30_000);
        order.Place(paidAt.AddMinutes(-1), paidAt.AddMinutes(14));
        order.MarkPaid(order.TotalAmount, paidAt);

        var store = Substitute.For<IOrderExecutionDispatchStore>();
        store.ExecuteSerializedAsync(
                order.Id,
                Arg.Any<Func<CancellationToken, Task<ApiResult<bool>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<bool>>>>()(CancellationToken.None));
        store.GetOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        store.GetCommandAsync(order.Id, 1, Arg.Any<CancellationToken>()).Returns((EdgeCommand?)null);
        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        var handler = new EscalateInitialDispatchFailureCommandHandler(
            store,
            publisher,
            Options.Create(new OrderExecutionDispatchOptions
            {
                InitialDispatchSupportEscalationMinutes = 15
            }));

        var result = await handler.HandleAsync(order.Id, "No compatible endpoint.");

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.Data);
        Assert.Equal(OrderStatus.FulfillmentIssue, order.Status);
        Assert.Contains("No compatible endpoint", order.Notes, StringComparison.Ordinal);
        await store.Received(1).AddOrderStatusHistoryAsync(
            Arg.Is<OrderStatusHistory>(history =>
                history.FromStatus == OrderStatus.ReadyForFulfillment &&
                history.ToStatus == OrderStatus.FulfillmentIssue),
            Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishOrderStatusChangedAsync(
            Arg.Is<OrderStatusChangedEvent>(evt =>
                evt.OrderId == order.Id &&
                evt.CustomerStatus == "SupportRequired"),
            Arg.Any<CancellationToken>());
    }
}
