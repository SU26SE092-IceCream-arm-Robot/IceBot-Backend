using Application.Payments.Abstractions;
using Application.Orders.Abstractions;
using Application.Payments.PaymentSessions.Mapping;
using Application.Payments.PaymentSessions.Results;
using Application.Shared.Wrappers;

namespace Application.Payments.PaymentSessions.Queries;

public sealed class GetOrderPaymentStatusQueryHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IOrderStore _orderStore;

    public GetOrderPaymentStatusQueryHandler(IPaymentStore paymentStore, IOrderStore orderStore)
    {
        _paymentStore = paymentStore;
        _orderStore = orderStore;
    }

    public async Task<ApiResult<PaymentStatusResult>> HandleAsync(
        GetOrderPaymentStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var paymentTransaction = await _paymentStore.GetLatestPaymentTransactionByOrderIdAsync(query.OrderId, cancellationToken);
        if (paymentTransaction is null)
        {
            return ApiResult<PaymentStatusResult>.Fail("Payment transaction not found.", 404);
        }

        var executionRecord = await _orderStore.GetLatestOrderExecutionRecordAsync(query.OrderId, cancellationToken);
        return ApiResult<PaymentStatusResult>.Success(
            PaymentStatusResultMapper.ToStatusResult(paymentTransaction, executionRecord));
    }
}
