using System.Text.Json;
using Application.Operations.Alerts.Notifications;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Support;
using Domain.Operations.Entities;
using Domain.Payments.Entities;

namespace Application.Payments.PaymentSessions.Notifications;

public interface IPaymentInterventionNotificationRecipientStore
{
    Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default);
}

public interface IPaymentInterventionNotifier
{
    Task NotifyIfRequiredAsync(
        PaymentTransaction payment,
        PaymentSessionReconciliationOutcome outcome,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentInterventionNotifier(
    IPaymentInterventionNotificationRecipientStore recipients,
    INotificationDeliveryStore deliveries) : IPaymentInterventionNotifier
{
    public async Task NotifyIfRequiredAsync(
        PaymentTransaction payment,
        PaymentSessionReconciliationOutcome outcome,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        if (!PaymentSessionInterventionPolicy.RequiresNotification(outcome) ||
            string.IsNullOrWhiteSpace(payment.LastErrorCode) ||
            !payment.Order.OrganizationId.HasValue ||
            !payment.Order.StoreId.HasValue)
        {
            return;
        }

        var interventionCode = payment.LastErrorCode.Trim().ToUpperInvariant();
        var accountIds = await recipients.ListRecipientAccountIdsAsync(
            payment.Order.OrganizationId.Value,
            payment.Order.StoreId.Value,
            payment.Order.KioskId,
            cancellationToken);

        foreach (var accountId in accountIds.Distinct())
        {
            var key = $"payment-intervention:{payment.Id:D}:{interventionCode}:account:{accountId:D}";
            if (await deliveries.ExistsByKeyAsync(key, cancellationToken))
            {
                continue;
            }

            var data = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["type"] = "payment_intervention",
                ["deliveryId"] = string.Empty,
                ["paymentTransactionId"] = payment.Id.ToString("D"),
                ["orderId"] = payment.OrderId.ToString("D"),
                ["kioskId"] = payment.Order.KioskId.ToString("D"),
                ["interventionCode"] = interventionCode
            });
            await deliveries.AddAsync(NotificationDelivery.CreatePush(
                payment.Order.OrganizationId.Value,
                payment.Order.StoreId.Value,
                payment.Order.KioskId,
                payment.Id,
                key,
                "payment_intervention",
                accountId,
                "Payment requires staff review",
                $"Order {payment.Order.OrderNumber} has a payment session requiring review.",
                data,
                observedAt), cancellationToken);
        }
    }

}
