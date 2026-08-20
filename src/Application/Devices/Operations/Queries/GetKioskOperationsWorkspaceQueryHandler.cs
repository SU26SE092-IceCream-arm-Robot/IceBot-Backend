using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Operations.Results;
using Application.Devices.Telemetry.Abstractions;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Tenants.Abstractions;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionExecution.Enums;

namespace Application.Devices.Operations.Queries;

public sealed record GetKioskOperationsWorkspaceQuery(Guid KioskId, CurrentUserContext UserContext);

public sealed class GetKioskOperationsWorkspaceQueryHandler(
    IKioskTelemetryStore telemetry,
    IKioskStore kiosks,
    IExecutionEndpointStore endpoints)
{
    public async Task<ApiResult<KioskOperationsWorkspaceResult>> HandleAsync(
        GetKioskOperationsWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await telemetry.GetKioskByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskOperationsWorkspaceResult>.Fail("Kiosk was not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.OperationsView,
                query.UserContext,
                kiosk.OrganizationId,
                kiosk.StoreId,
                kiosk.Id))
        {
            return ApiResult<KioskOperationsWorkspaceResult>.Fail("Access denied.", 403);
        }

        var storeTask = kiosks.GetStoreByIdAsync(kiosk.StoreId, cancellationToken);
        var connectivityTask = telemetry.GetConnectivityAsync(kiosk.Id, cancellationToken);
        var heartbeatTask = telemetry.ListHeartbeatsAsync(kiosk.Id, null, null, null, 1, 1, cancellationToken);
        var eventTask = telemetry.ListDeviceEventsAsync(kiosk.Id, null, null, null, null, 1, 1, cancellationToken);
        var endpointTask = endpoints.ListAsync(null, null, kiosk.Id, null, null, cancellationToken);

        await Task.WhenAll(storeTask, connectivityTask, heartbeatTask, eventTask, endpointTask);
        var endpointItems = endpointTask.Result;
        var readinessByEndpoint = (await endpoints.ListReadinessAsync(
                endpointItems.Select(endpoint => endpoint.Id),
                cancellationToken))
            .ToDictionary(projection => projection.KioskExecutionEndpointId);

        var activeEndpoints = endpointItems
            .Where(endpoint => endpoint.Status == KioskExecutionEndpointStatus.Active)
            .OrderBy(endpoint => endpoint.EndpointCode, StringComparer.Ordinal)
            .ToArray();
        var readyEndpoints = activeEndpoints
            .Where(endpoint => readinessByEndpoint.TryGetValue(endpoint.Id, out var readiness) &&
                readiness.Readiness == ExecutionReadinessState.Ready)
            .ToArray();
        var selected = readyEndpoints.Length == 1 ? readyEndpoints[0] : null;
        readinessByEndpoint.TryGetValue(selected?.Id ?? Guid.Empty, out var selectedReadiness);

        var latestHeartbeat = heartbeatTask.Result.FirstOrDefault();
        var latestEvent = eventTask.Result.FirstOrDefault();
        var connectivity = connectivityTask.Result;

        return ApiResult<KioskOperationsWorkspaceResult>.Success(new KioskOperationsWorkspaceResult
        {
            Kiosk = new KioskOperationsWorkspaceKioskResult
            {
                Id = kiosk.Id,
                OrganizationId = kiosk.OrganizationId,
                StoreId = kiosk.StoreId,
                StoreName = storeTask.Result?.Name ?? string.Empty,
                Code = kiosk.Code,
                Name = kiosk.Name,
                LifecycleStatus = kiosk.Status.ToString(),
                OperationalState = kiosk.OperationalState.ToString(),
                OperationalStateReason = kiosk.OperationalStateReason,
                OperationalStateChangedAt = kiosk.OperationalStateChangedAt
            },
            Connectivity = new KioskOperationsWorkspaceConnectivityResult
            {
                Status = connectivity?.Status.ToString() ?? "Unknown",
                LastHeartbeatAt = latestHeartbeat?.ReportedAt ?? kiosk.LastOnlineAt,
                LatestHeartbeatStatus = latestHeartbeat?.Status.ToString(),
                LatestHeartbeatReportedAt = latestHeartbeat?.ReportedAt,
                LatestEventType = latestEvent?.EventType,
                LatestEventSeverity = latestEvent?.Severity.ToString(),
                LatestEventAt = latestEvent?.OccurredAt
            },
            Execution = new KioskOperationsWorkspaceExecutionResult
            {
                EndpointCount = endpointItems.Count,
                ActiveEndpointCount = activeEndpoints.Length,
                ReadyEndpointCount = readyEndpoints.Length,
                HasMultipleReadyEndpoints = readyEndpoints.Length > 1,
                SoleReadyEndpoint = selected is null ? null : new KioskOperationsWorkspaceEndpointResult
                {
                    EndpointId = selected.Id,
                    EndpointCode = selected.EndpointCode,
                    ExecutionProfile = selected.ExecutionProfile.ToString(),
                    Readiness = selectedReadiness?.Readiness.ToString(),
                    Activity = selectedReadiness?.Activity.ToString(),
                    Safety = selectedReadiness?.Safety.ToString(),
                    FaultCode = selectedReadiness?.FaultCode,
                    ReportedAt = selectedReadiness?.ExecutorReportedAt
                }
            },
            Configuration = new KioskOperationsWorkspaceConfigurationResult
            {
                ActiveReleaseId = selected?.ActiveConfigurationReleaseId ?? selected?.ActiveArtifactSetReleaseId,
                ActiveDeploymentId = selected?.ActiveConfigurationDeploymentId ?? selected?.ActiveArtifactSetDeploymentId,
                ActiveConfigurationReportedAt = selected?.ActiveConfigurationCloudReceivedAt ?? selected?.ActiveArtifactSetCloudReceivedAt
            },
            AvailableActions = new KioskOperationsWorkspaceActionsResult
            {
                CanManageKiosk = ScopeAccessRules.CanAccessScopedRow(
                    ScopeRoleSets.KiosksOperationsManage,
                    query.UserContext,
                    kiosk.OrganizationId,
                    kiosk.StoreId,
                    kiosk.Id),
                CanViewDeployment = ScopeAccessRules.CanAccessScopedRow(
                    ScopeRoleSets.DeploymentRead,
                    query.UserContext,
                    kiosk.OrganizationId,
                    kiosk.StoreId,
                    kiosk.Id)
            }
        });
    }
}
