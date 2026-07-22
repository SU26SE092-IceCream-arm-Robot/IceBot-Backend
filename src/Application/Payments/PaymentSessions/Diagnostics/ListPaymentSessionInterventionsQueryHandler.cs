using Application.Payments.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Payments.Enums;
using Application.Payments.PaymentSessions.Support;

namespace Application.Payments.PaymentSessions.Diagnostics;

public sealed class ListPaymentSessionInterventionsQueryHandler(IPaymentStore paymentStore)
{
    public async Task<PagedResult<PaymentSessionInterventionResult>> HandleAsync(
        ListPaymentSessionInterventionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.PaymentsManage, query.UserContext);
        var observedAt = DateTimeOffset.UtcNow;

        var totalCount = await paymentStore.CountPaymentSessionInterventionsAsync(
            observedAt,
            query.Provider,
            query.InterventionCode,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);
        var payments = await paymentStore.ListPaymentSessionInterventionsAsync(
            observedAt,
            query.Provider,
            query.InterventionCode,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<PaymentSessionInterventionResult>.Success(
            payments.Select(payment => new PaymentSessionInterventionResult(
                payment.Id,
                payment.OrderId,
                payment.Order.OrderNumber,
                payment.Order.OrganizationId,
                payment.Order.StoreId,
                payment.Order.KioskId,
                payment.Provider,
                payment.ProviderOrderCode!,
                payment.Status,
                payment.Amount,
                payment.Currency,
                payment.LastErrorCode!,
                payment.LastErrorMessage,
                payment.RetryCount,
                payment.MaxRetries,
                payment.RequestedAt,
                payment.LastAttemptAt,
                payment.NextRetryAt,
                PaymentSessionInterventionPolicy.CanReconcile(payment, observedAt))),
            totalCount,
            pageNumber,
            pageSize,
            "Payment-session interventions retrieved.");
    }
}
