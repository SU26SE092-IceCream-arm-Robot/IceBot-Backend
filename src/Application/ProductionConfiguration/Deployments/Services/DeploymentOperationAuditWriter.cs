using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.Operations.OperationLogs.Abstractions;
using Application.Tenants;
using Domain.Common.Enums;
using Domain.Operations.Entities;

namespace Application.ProductionConfiguration.Deployments.Services;

public sealed class DeploymentOperationAuditWriter(IOperationLogStore operationLogs)
{
    public Task WriteRequestedAsync(
        CurrentUserContext user,
        IReadOnlyCollection<string> allowedRoles,
        string operation,
        string reason,
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        Guid endpointId,
        Guid deploymentId,
        Guid edgeCommandId,
        Guid configurationReleaseId,
        string releaseChecksum,
        Guid? observedActiveDeploymentId,
        Guid? rollbackTargetDeploymentId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var authorization = ScopeAccessRules.GetAuthorizingScopeSnapshots(
            allowedRoles, user, organizationId, storeId, kioskId);

        return operationLogs.AddAsync(new OperationLog
        {
            AccountId = user.AccountId,
            KioskId = kioskId,
            CorrelationId = deploymentId,
            CausationId = rollbackTargetDeploymentId,
            Action = operation,
            Category = "ProductionConfiguration",
            Severity = SeverityLevel.Info,
            Message = operation == "ConfigurationRollbackRequested"
                ? "A configuration rollback was requested."
                : "A configuration deployment was requested.",
            PayloadJson = JsonSerializer.Serialize(new
            {
                Reason = reason,
                Authorization = authorization,
                OrganizationId = organizationId,
                StoreId = storeId,
                KioskId = kioskId,
                KioskExecutionEndpointId = endpointId,
                DeploymentId = deploymentId,
                EdgeCommandId = edgeCommandId,
                ConfigurationReleaseId = configurationReleaseId,
                ReleaseChecksum = releaseChecksum,
                ObservedActiveDeploymentId = observedActiveDeploymentId,
                RollbackTargetDeploymentId = rollbackTargetDeploymentId,
                InitialDeploymentStatus = "Pending"
            }),
            OccurredAt = occurredAt,
            OriginNodeId = Guid.Empty,
            Version = 1,
            SyncedAt = occurredAt
        }, cancellationToken);
    }
}
