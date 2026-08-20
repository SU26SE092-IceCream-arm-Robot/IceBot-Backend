using Application.Identity.Tokens.Claims;
using Application.Payments.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.PaymentSessions.Diagnostics;

public sealed class GetOrderPaymentDiagnosticsQueryHandler(IPaymentStore paymentStore)
{
    public async Task<ApiResult<IReadOnlyCollection<PaymentSessionDiagnosticsResult>>> HandleAsync(
        Guid orderId,
        CurrentUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        var order = await paymentStore.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<IReadOnlyCollection<PaymentSessionDiagnosticsResult>>.Fail("Order not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.PaymentDiagnosticsView,
                userContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
        {
            return ApiResult<IReadOnlyCollection<PaymentSessionDiagnosticsResult>>.Fail("Access denied.", 403);
        }

        var payments = await paymentStore.ListPaymentTransactionsByOrderIdAsync(orderId, cancellationToken);
        return ApiResult<IReadOnlyCollection<PaymentSessionDiagnosticsResult>>.Success(
            payments.Select(payment => new PaymentSessionDiagnosticsResult(
                payment.Id,
                payment.OrderId,
                payment.Provider,
                payment.ProviderOrderCode,
                payment.ProviderPaymentLinkId,
                payment.ProviderTransactionId,
                payment.Status,
                payment.Amount,
                payment.PaidAmount,
                payment.Currency,
                payment.ProviderStatus,
                payment.RequestedAt,
                payment.ExpiresAt,
                payment.LastAttemptAt,
                payment.NextRetryAt,
                payment.RetryCount,
                payment.MaxRetries,
                payment.LastErrorCode,
                payment.LastErrorMessage)).ToArray());
    }
}
