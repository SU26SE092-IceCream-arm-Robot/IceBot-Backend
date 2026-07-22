using Domain.Common;
using Domain.Operations.Enums;

namespace Domain.Operations.Entities;

public sealed class NotificationDelivery : GuidEntity, IAuditable
{
    public Guid OrganizationId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? KioskId { get; private set; }
    public Guid? SubjectId { get; private set; }
    public string DeliveryKey { get; private set; } = null!;
    public string NotificationType { get; private set; } = null!;
    public Guid RecipientAccountId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string DataJson { get; private set; } = "{}";
    public NotificationDeliveryStatus Status { get; private set; } = NotificationDeliveryStatus.Pending;
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; } = 5;
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }

    public static NotificationDelivery CreatePush(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? subjectId,
        string deliveryKey,
        string notificationType,
        Guid recipientAccountId,
        string title,
        string body,
        string dataJson,
        DateTimeOffset now,
        int maxAttempts = 5)
    {
        if (organizationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(deliveryKey) || string.IsNullOrWhiteSpace(notificationType) ||
            recipientAccountId == Guid.Empty || string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(dataJson) || maxAttempts <= 0)
        {
            throw new DomainRuleException("Notification delivery identity, recipient, content, and retry policy are required.");
        }

        return new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            SubjectId = subjectId,
            DeliveryKey = deliveryKey.Trim(),
            NotificationType = notificationType.Trim(),
            RecipientAccountId = recipientAccountId,
            Title = title.Trim(),
            Body = body.Trim(),
            DataJson = dataJson,
            NextAttemptAt = now,
            MaxAttempts = maxAttempts,
            CreatedAt = now
        };
    }

    public bool CanBeClaimed(DateTimeOffset now, TimeSpan processingTimeout) =>
        AttemptCount < MaxAttempts &&
        ((Status is NotificationDeliveryStatus.Pending or NotificationDeliveryStatus.Failed && NextAttemptAt <= now) ||
         (Status == NotificationDeliveryStatus.Processing && ProcessingStartedAt <= now - processingTimeout));

    public bool IsStaleProcessing(DateTimeOffset now, TimeSpan processingTimeout) =>
        Status == NotificationDeliveryStatus.Processing &&
        ProcessingStartedAt <= now - processingTimeout;

    public void MarkProcessing(DateTimeOffset now, TimeSpan processingTimeout)
    {
        if (!CanBeClaimed(now, processingTimeout))
        {
            throw new DomainRuleException("Notification delivery is not due for processing.");
        }

        AttemptCount++;
        LastAttemptAt = now;
        ProcessingStartedAt = now;
        Status = NotificationDeliveryStatus.Processing;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        if (Status != NotificationDeliveryStatus.Processing)
        {
            throw new DomainRuleException("Only a processing notification delivery can be completed.");
        }

        Status = NotificationDeliveryStatus.Delivered;
        DeliveredAt = now;
        ProcessingStartedAt = null;
        LastErrorCode = null;
        LastErrorMessage = null;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset nextAttemptAt)
    {
        if (Status != NotificationDeliveryStatus.Processing)
        {
            throw new DomainRuleException("Only a processing notification delivery can fail.");
        }

        LastErrorCode = Normalize(errorCode, "Notification failure code");
        LastErrorMessage = Normalize(errorMessage, "Notification failure message");
        ProcessingStartedAt = null;
        NextAttemptAt = nextAttemptAt;
        Status = AttemptCount >= MaxAttempts
            ? NotificationDeliveryStatus.PermanentFailure
            : NotificationDeliveryStatus.Failed;
    }

    public void MarkPermanentFailure(string errorCode, string errorMessage)
    {
        if (Status != NotificationDeliveryStatus.Processing)
        {
            throw new DomainRuleException("Only a processing notification delivery can fail.");
        }

        LastErrorCode = Normalize(errorCode, "Notification failure code");
        LastErrorMessage = Normalize(errorMessage, "Notification failure message");
        ProcessingStartedAt = null;
        Status = NotificationDeliveryStatus.PermanentFailure;
    }

    public void Requeue(Guid actorId, DateTimeOffset now)
    {
        if (Status != NotificationDeliveryStatus.PermanentFailure)
            throw new DomainRuleException("Only a permanently failed notification delivery can be requeued.");
        if (actorId == Guid.Empty)
            throw new DomainRuleException("Notification requeue actor is required.");

        Status = NotificationDeliveryStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = now;
        ProcessingStartedAt = null;
        LastAttemptAt = null;
        DeliveredAt = null;
        LastErrorCode = null;
        LastErrorMessage = null;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    private static string Normalize(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainRuleException($"{field} is required.")
            : value.Trim();
}
