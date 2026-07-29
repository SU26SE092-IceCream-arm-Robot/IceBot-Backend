using System.Text.Json;
using Application.Operations.Alerts.Notifications;
using Domain.Operations.Entities;

namespace Application.ProductionConfiguration.Deployments.Notifications;

public sealed class DeploymentFailureNotificationOptions
{
    public const string SectionName = "DeploymentFailureNotification";
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 100;
}

public sealed record DeploymentFailureNotificationCandidate(
    Guid DeploymentId,
    Guid OrganizationId,
    Guid StoreId,
    Guid KioskId,
    string Profile,
    string FailureCode,
    DateTimeOffset FailedAt);

public interface IDeploymentFailureNotificationStore
{
    Task<IReadOnlyList<Guid>> ListPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<DeploymentFailureNotificationCandidate?> GetCandidateAsync(Guid deploymentId,
        CancellationToken cancellationToken = default);
}

public sealed class DeploymentFailureNotificationService(
    IDeploymentFailureNotificationStore failures,
    IOperationalAlertNotificationRecipientStore recipients,
    INotificationDeliveryStore deliveries)
{
    public Task<IReadOnlyList<Guid>> ListPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default) =>
        failures.ListPendingIdsAsync(batchSize, cancellationToken);

    public Task ProcessAsync(Guid deploymentId, DateTimeOffset observedAt,
        CancellationToken cancellationToken = default) =>
        deliveries.ExecuteInTransactionAsync(async ct =>
        {
            await deliveries.AcquireLockAsync(deploymentId, ct);
            var candidate = await failures.GetCandidateAsync(deploymentId, ct);
            if (candidate is null) return true;
            var accountIds = await recipients.ListRecipientAccountIdsAsync(
                candidate.OrganizationId, candidate.StoreId, candidate.KioskId, ct);
            foreach (var accountId in accountIds.Distinct())
            {
                var key = $"deployment-failed:{candidate.DeploymentId:D}:account:{accountId:D}";
                if (await deliveries.ExistsByKeyAsync(key, ct)) continue;
                var data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["type"] = "deployment_failed",
                    ["deliveryId"] = string.Empty,
                    ["deploymentId"] = candidate.DeploymentId.ToString("D"),
                    ["kioskId"] = candidate.KioskId.ToString("D"),
                    ["profile"] = candidate.Profile,
                    ["failureCode"] = candidate.FailureCode
                });
                await deliveries.AddAsync(NotificationDelivery.CreatePush(
                    candidate.OrganizationId, candidate.StoreId, candidate.KioskId,
                    candidate.DeploymentId, key, "deployment_failed", accountId,
                    "Configuration deployment failed",
                    $"A {candidate.Profile} deployment failed at kiosk {candidate.KioskId:D}.",
                    data, observedAt), ct);
            }
            await deliveries.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
}
