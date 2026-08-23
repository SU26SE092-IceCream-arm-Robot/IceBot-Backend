using Application.Abstractions.Realtime;
using Application.ClientDevices;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder;
using Application.Orders.PlaceOrder.Commands;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Services;
using Application.SalesCatalog.Admission.Abstractions;
using Application.SalesCatalog.Admission.Services;
using Application.Tenants.Kiosks.Rules;
using Application.Devices.Telemetry;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Orders;

public sealed class PlaceOrderRequestValidationTests
{
    [Fact]
    public async Task InvalidRequest_ReturnsAllStableFieldPathsBeforeAccessingPersistence()
    {
        var orderStore = Substitute.For<IOrderStore>();
        var handler = new PlaceOrderCommandHandler(
            orderStore,
            Substitute.For<IRealtimeNotificationPublisher>(),
            new PlaceOrderItemAppender(orderStore, Substitute.For<IMenuItemOperationalAdmissionEvaluator>()),
            Options.Create(new OrderPaymentWindowOptions()),
            new KioskSalesAdmissionEvaluator(
                Substitute.For<IOperationalAdmissionReadStore>(),
                Options.Create(new KioskSalesAdmissionOptions()),
                Options.Create(new EdgeTelemetryIngestionOptions())),
            Options.Create(new ClientDeviceRuntimeOptions { MaxQuantityPerLine = 2, MaxTotalUnits = 3 }));

        var result = await handler.HandleAsync(new PlaceOrderCommand
        {
            KioskId = Guid.NewGuid(),
            SourceClientDeviceId = Guid.NewGuid(),
            IdempotencyKey = "checkout-validation",
            Request = new PlaceOrderRequest
            {
                ClientTotalAmount = 0,
                Items =
                [
                    new PlaceOrderItemRequest
                    {
                        Quantity = 3,
                        ClientLineId = "line",
                        SelectedOptions = [new SelectedProductOptionRequest(), new SelectedProductOptionRequest()]
                    },
                    new PlaceOrderItemRequest
                    {
                        MenuItemId = Guid.NewGuid(),
                        Quantity = 1,
                        ClientLineId = "line",
                        SelectedOptions = null!
                    }
                ]
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(result.BusinessError);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains("clientTotalAmount", result.ValidationErrors.Keys);
        Assert.Contains("items[0].menuItemId", result.ValidationErrors.Keys);
        Assert.Contains("items[0].quantity", result.ValidationErrors.Keys);
        Assert.Contains("items[0].selectedOptions[0].productOptionId", result.ValidationErrors.Keys);
        Assert.Contains("items[0].selectedOptions[1].productOptionId", result.ValidationErrors.Keys);
        Assert.Contains("items[1].clientLineId", result.ValidationErrors.Keys);
        Assert.Contains("items[1].selectedOptions", result.ValidationErrors.Keys);
        await orderStore.DidNotReceive().ExecuteCheckoutTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<Application.Orders.PlaceOrder.Results.OrderResult>>>>(),
            Arg.Any<CancellationToken>());
    }
}
