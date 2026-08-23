using Application.ProductionConfiguration.Deployments.Notifications;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.ProductionConfiguration.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProductionConfiguration.Persistence.Deployments;

public sealed class DeploymentFailureNotificationStore(IceBotDbContext db) : IDeploymentFailureNotificationStore
{
    public async Task<IReadOnlyList<Guid>> ListPendingIdsAsync(int batchSize,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(batchSize, 1, 500);
        return await FullEdgeCandidates()
            .Select(x => new { x.Id, FailedAt = x.CloudReceivedAt ?? x.RequestedAt })
            .Concat(ControllerCandidates()
                .Select(x => new { x.Id, FailedAt = x.CloudReceivedAt ?? x.RequestedAt }))
            .OrderBy(x => x.FailedAt)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeploymentFailureNotificationCandidate?> GetCandidateAsync(Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        var fullEdge = await FullEdgeCandidates().Where(x => x.Id == deploymentId)
            .Select(x => new DeploymentFailureNotificationCandidate(
                x.Id, x.OrganizationId, x.KioskExecutionEndpoint.Kiosk.StoreId, x.KioskId,
                "FullEdge", x.FailureCode!, x.CloudReceivedAt ?? x.RequestedAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (fullEdge is not null) return fullEdge;
        return await ControllerCandidates().Where(x => x.Id == deploymentId)
            .Select(x => new DeploymentFailureNotificationCandidate(
                x.Id, x.OrganizationId, x.KioskExecutionEndpoint.Kiosk.StoreId, x.KioskId,
                "LowCostController", x.FailureCode!, x.CloudReceivedAt ?? x.RequestedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Domain.ProductionConfiguration.Entities.KioskConfigurationDeployment> FullEdgeCandidates()
    {
        var businessRecipients = EligibleBusinessRecipients();
        var technicianRecipients = EligibleTechnicianRecipients();
        return db.KioskConfigurationDeployments.AsNoTracking().Where(x =>
            x.Status == KioskConfigurationDeploymentStatus.Failed && x.FailureCode != null &&
            !db.NotificationDeliveries.Any(delivery =>
                delivery.NotificationType == "deployment_failed" && delivery.SubjectId == x.Id) &&
            (businessRecipients.Any(accountRole =>
                ((accountRole.Role.Code == "Manager" &&
                  (accountRole.StoreId == x.KioskExecutionEndpoint.Kiosk.StoreId ||
                   accountRole.OrganizationId == x.OrganizationId)) ||
                 (accountRole.Role.Code == "OrgAdmin" && accountRole.OrganizationId == x.OrganizationId))) ||
              technicianRecipients.Any(grant =>
                 (grant.KioskId == x.KioskId ||
                  grant.StoreId == x.KioskExecutionEndpoint.Kiosk.StoreId))));
    }

    private IQueryable<Domain.ProductionConfiguration.Entities.ControllerArtifactSetDeployment> ControllerCandidates()
    {
        var businessRecipients = EligibleBusinessRecipients();
        var technicianRecipients = EligibleTechnicianRecipients();
        return db.ControllerArtifactSetDeployments.AsNoTracking().Where(x =>
            x.Status == ControllerArtifactSetDeploymentStatus.Failed && x.FailureCode != null &&
            !db.NotificationDeliveries.Any(delivery =>
                delivery.NotificationType == "deployment_failed" && delivery.SubjectId == x.Id) &&
            (businessRecipients.Any(accountRole =>
                ((accountRole.Role.Code == "Manager" &&
                  (accountRole.StoreId == x.KioskExecutionEndpoint.Kiosk.StoreId ||
                   accountRole.OrganizationId == x.OrganizationId)) ||
                 (accountRole.Role.Code == "OrgAdmin" && accountRole.OrganizationId == x.OrganizationId))) ||
              technicianRecipients.Any(grant =>
                 (grant.KioskId == x.KioskId ||
                  grant.StoreId == x.KioskExecutionEndpoint.Kiosk.StoreId))));
    }

    private IQueryable<AccountRole> EligibleBusinessRecipients() =>
        db.AccountRoles.AsNoTracking().Where(accountRole =>
            accountRole.IsActive &&
            accountRole.Account.Status == AccountStatus.Active &&
            accountRole.Account.DeletedAt == null &&
            accountRole.Account.NotificationDevices.Any(device =>
                device.DeletedAt == null && device.InvalidatedAt == null && device.PushToken != null));

    private IQueryable<TechnicianSupportGrant> EligibleTechnicianRecipients() =>
        db.TechnicianSupportGrants.AsNoTracking().Where(grant =>
            grant.IsActive && grant.DeletedAt == null &&
            grant.Account.PlatformTechnicianProfile != null &&
            grant.Account.Status == AccountStatus.Active &&
            grant.Account.DeletedAt == null &&
            grant.Account.NotificationDevices.Any(device =>
                device.DeletedAt == null && device.InvalidatedAt == null && device.PushToken != null));
}
