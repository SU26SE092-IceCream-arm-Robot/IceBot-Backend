using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Mapping;
using Application.Payments.PaymentSessions.Results;
using Application.Shared.Wrappers;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Payments.PaymentSessions.Queries;

public sealed class GetPaymentTransactionStatusQueryHandler
{
    private readonly IPaymentStore _paymentStore;

    public GetPaymentTransactionStatusQueryHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<ApiResult<PaymentStatusResult>> HandleAsync(
        GetPaymentTransactionStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var paymentTransaction = await _paymentStore.GetPaymentTransactionByIdAsync(query.PaymentTransactionId, cancellationToken);
        if (paymentTransaction is null)
        {
            return ApiResult<PaymentStatusResult>.Fail("Payment transaction not found.", 404);
        }

        return ApiResult<PaymentStatusResult>.Success(PaymentStatusResultMapper.ToStatusResult(paymentTransaction));
    }
}
