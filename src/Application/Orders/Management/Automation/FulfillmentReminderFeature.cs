using System.Text.Json;
using Application.Operations.Alerts.Notifications;
using Domain.Operations.Entities;

namespace Application.Orders.Management.Automation;

public sealed class FulfillmentReminderOptions
{
    public const string SectionName = "FulfillmentReminder";
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 100;
}

public sealed record FulfillmentReminderCandidate(
    Guid OrderItemId,
    Guid OrderId,
    string OrderNumber,
    Guid OrganizationId,
    Guid StoreId,
    Guid KioskId,
    DateTimeOffset PaidAt,
    DateTimeOffset ExpectedReadyAt);

public interface IFulfillmentReminderStore
{
    Task<IReadOnlyList<Guid>> ListOverdueItemIdsAsync(
        DateTimeOffset observedAt,
        int batchSize,
        CancellationToken cancellationToken = default);
    Task<FulfillmentReminderCandidate?> GetOverdueCandidateAsync(
        Guid orderItemId,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default);
}

public sealed class FulfillmentReminderService(
    IFulfillmentReminderStore reminders,
    INotificationDeliveryStore deliveries)
{
    public Task<IReadOnlyList<Guid>> ListDueIdsAsync(
        DateTimeOffset observedAt,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        reminders.ListOverdueItemIdsAsync(observedAt, batchSize, cancellationToken);

    public Task ProcessAsync(
        Guid orderItemId,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default) =>
        deliveries.ExecuteInTransactionAsync(async ct =>
        {
            await deliveries.AcquireLockAsync(orderItemId, ct);
            var candidate = await reminders.GetOverdueCandidateAsync(orderItemId, observedAt, ct);
            if (candidate is null) return true;

            var recipients = await reminders.ListRecipientAccountIdsAsync(
                candidate.OrganizationId, candidate.StoreId, candidate.KioskId, ct);
            foreach (var accountId in recipients.Distinct())
            {
                var deliveryKey = $"fulfillment-overdue:{candidate.OrderItemId:D}:paid:{candidate.PaidAt.UtcTicks}:account:{accountId:D}";
                if (await deliveries.ExistsByKeyAsync(deliveryKey, ct)) continue;

                var data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["type"] = "fulfillment_overdue",
                    ["deliveryId"] = string.Empty,
                    ["orderId"] = candidate.OrderId.ToString("D"),
                    ["orderItemId"] = candidate.OrderItemId.ToString("D"),
                    ["kioskId"] = candidate.KioskId.ToString("D")
                });
                await deliveries.AddAsync(NotificationDelivery.CreatePush(
                    candidate.OrganizationId,
                    candidate.StoreId,
                    candidate.KioskId,
                    candidate.OrderItemId,
                    deliveryKey,
                    "fulfillment_overdue",
                    accountId,
                    "Fulfillment item overdue",
                    $"Order {candidate.OrderNumber} has an overdue fulfillment item.",
                    data,
                    observedAt), ct);
            }

            await deliveries.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
}
