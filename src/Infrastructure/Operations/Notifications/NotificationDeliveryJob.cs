using System.Text.Json;
using Application.Identity.NotificationDevices.Delivery;
using Application.Operations.Alerts.Notifications;
using Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Operations.Notifications;

public sealed class NotificationDeliveryJob(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDeliveryOptions> options,
    ILogger<NotificationDeliveryJob> logger) : BackgroundService
{
    private readonly NotificationDeliveryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        do
        {
            await DeliverBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DeliverBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<INotificationDeliveryStore>();
            var now = DateTimeOffset.UtcNow;
            var ids = await store.ListDueIdsAsync(
                now,
                now.AddSeconds(-_options.ProcessingTimeoutSeconds),
                _options.BatchSize,
                cancellationToken);
            NotificationDeliveryMetrics.RecordBatchSize(ids.Count);

            foreach (var id in ids)
            {
                try
                {
                    await DeliverOneAsync(id, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Notification delivery failed for delivery {DeliveryId}.", id);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Notification delivery batch failed.");
        }
    }

    private async Task DeliverOneAsync(Guid id, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<INotificationDeliveryStore>();
        var now = DateTimeOffset.UtcNow;
        var timeout = TimeSpan.FromSeconds(_options.ProcessingTimeoutSeconds);
        var delivery = await store.ExecuteInTransactionAsync(async ct =>
        {
            await store.AcquireLockAsync(id, ct);
            var candidate = await store.GetByIdAsync(id, ct);
            if (candidate is null) return null;
            if (candidate.IsStaleProcessing(now, timeout) && candidate.AttemptCount >= candidate.MaxAttempts)
            {
                candidate.MarkPermanentFailure(
                    "PROCESSING_TIMEOUT_EXHAUSTED",
                    "The final notification attempt did not record a delivery outcome before its processing lease expired.");
                await store.SaveChangesAsync(ct);
                return null;
            }
            if (!candidate.CanBeClaimed(now, timeout)) return null;
            NotificationDeliveryMetrics.RecordClaim(candidate.NextAttemptAt, now);
            candidate.MarkProcessing(now, timeout);
            await store.SaveChangesAsync(ct);
            return candidate;
        }, cancellationToken);
        if (delivery is null) return;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(delivery.DataJson) ?? [];
            data["deliveryId"] = delivery.Id.ToString("D");
            var sender = scope.ServiceProvider.GetRequiredService<IAccountPushNotificationSender>();
            var result = await sender.SendAsync(
                new AccountPushNotification(
                    delivery.RecipientAccountId,
                    delivery.Title,
                    delivery.Body,
                    data),
                cancellationToken);

            if (result.SucceededCount > 0)
            {
                delivery.MarkDelivered(DateTimeOffset.UtcNow);
            }
            else if (result.TargetCount == 0 || result.InvalidatedTokenCount == result.TargetCount)
            {
                delivery.MarkPermanentFailure("NO_ACTIVE_PUSH_TARGET", "No active push-notification target remains for the account.");
            }
            else
            {
                delivery.MarkFailed(
                    "PUSH_DELIVERY_FAILED",
                    "Push provider did not deliver the notification to an active target.",
                    NextAttemptAt(delivery.AttemptCount));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException exception)
        {
            delivery.MarkPermanentFailure("INVALID_NOTIFICATION_PAYLOAD", exception.Message);
        }
        catch (Exception exception)
        {
            delivery.MarkFailed("NOTIFICATION_TRANSPORT_FAILED", exception.Message, NextAttemptAt(delivery.AttemptCount));
        }

        await store.SaveChangesAsync(cancellationToken);
        NotificationDeliveryMetrics.RecordOutcome(delivery.Status, delivery.NotificationType);
    }

    private DateTimeOffset NextAttemptAt(int attemptCount)
    {
        var multiplier = Math.Pow(2, Math.Clamp(attemptCount - 1, 0, 8));
        return DateTimeOffset.UtcNow.AddSeconds(_options.BaseRetryDelaySeconds * multiplier);
    }
}
