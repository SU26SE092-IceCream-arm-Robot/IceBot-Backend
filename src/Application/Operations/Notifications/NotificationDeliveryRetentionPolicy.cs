namespace Application.Operations.Notifications;

public static class NotificationDeliveryRetentionPolicy
{
    // These source workflows can be reconciled again long after delivery. Keep
    // their delivery key as durable business-event idempotency evidence.
    public static readonly string[] DurableEvidenceNotificationTypes =
    [
        "deployment_failed",
        "fulfillment_overdue",
        "payment_intervention"
    ];
}
