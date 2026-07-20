using Application.Operations.Alerts.Notifications;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Notifications;
using Domain.Operations.Entities;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class PaymentInterventionNotifierTests
{
    [Fact]
    public async Task RequiredIntervention_EnqueuesOncePerRecipientAndCode()
    {
        var accountId = Guid.NewGuid();
        var payment = CreatePayment("AWAITING_SIGNED_WEBHOOK");
        var recipients = Substitute.For<IPaymentInterventionNotificationRecipientStore>();
        recipients.ListRecipientAccountIdsAsync(
                payment.Order.OrganizationId!.Value,
                payment.Order.StoreId!.Value,
                payment.Order.KioskId,
                Arg.Any<CancellationToken>())
            .Returns([accountId, accountId]);
        var deliveries = Substitute.For<INotificationDeliveryStore>();
        var notifier = new PaymentInterventionNotifier(recipients, deliveries);

        await notifier.NotifyIfRequiredAsync(
            payment, PaymentSessionReconciliationOutcome.AwaitingWebhook, DateTimeOffset.UtcNow);

        await deliveries.Received(1).AddAsync(
            Arg.Is<NotificationDelivery>(delivery =>
                delivery.SubjectId == payment.Id &&
                delivery.NotificationType == "payment_intervention" &&
                delivery.DeliveryKey ==
                $"payment-intervention:{payment.Id:D}:AWAITING_SIGNED_WEBHOOK:account:{accountId:D}"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(PaymentSessionReconciliationOutcome.RetryScheduled)]
    [InlineData(PaymentSessionReconciliationOutcome.Restored)]
    [InlineData(PaymentSessionReconciliationOutcome.NotFound)]
    public async Task NonInterventionOutcome_DoesNotEnqueue(PaymentSessionReconciliationOutcome outcome)
    {
        var recipients = Substitute.For<IPaymentInterventionNotificationRecipientStore>();
        var deliveries = Substitute.For<INotificationDeliveryStore>();
        var notifier = new PaymentInterventionNotifier(recipients, deliveries);

        await notifier.NotifyIfRequiredAsync(CreatePayment("PROVIDER_LOOKUP_FAILED"), outcome, DateTimeOffset.UtcNow);

        await deliveries.DidNotReceive().AddAsync(
            Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingDeliveryKey_IsIdempotentNoOp()
    {
        var accountId = Guid.NewGuid();
        var payment = CreatePayment("PROVIDER_LOOKUP_FAILED");
        var recipients = Substitute.For<IPaymentInterventionNotificationRecipientStore>();
        recipients.ListRecipientAccountIdsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([accountId]);
        var deliveries = Substitute.For<INotificationDeliveryStore>();
        deliveries.ExistsByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var notifier = new PaymentInterventionNotifier(recipients, deliveries);

        await notifier.NotifyIfRequiredAsync(
            payment, PaymentSessionReconciliationOutcome.RetryExhausted, DateTimeOffset.UtcNow);

        await deliveries.DidNotReceive().AddAsync(
            Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>());
    }

    private static PaymentTransaction CreatePayment(string interventionCode)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-PAYMENT-REVIEW",
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            KioskId = Guid.NewGuid()
        };
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "PayOS",
            LastErrorCode = interventionCode
        };
    }
}
