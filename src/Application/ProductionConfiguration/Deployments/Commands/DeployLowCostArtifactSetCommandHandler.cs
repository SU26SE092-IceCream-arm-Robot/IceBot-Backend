using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs;
using Domain.Devices.ExecutionEndpoints;
using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.ProductionConfiguration.Deployments.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration.Deployments.Services;

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class DeployLowCostArtifactSetCommandHandler
{
    private readonly IConfigurationDeploymentStore _deploymentStore;
    private readonly IConfigurationReleaseStore _releaseStore;
    private readonly IEdgeCommandStore _edgeCommandStore;
    private readonly LowCostControllerCapacityOptions _capacity;
    private readonly IEdgeCommandWakeUpPublisher _wakeUpPublisher;
    private readonly ProductionInventoryReadinessGuard _inventoryReadiness;
    private readonly DeploymentValidationService? _deploymentValidation;
    private readonly IConfigurationDeploymentPreviewService? _deploymentPreview;
    private readonly DeploymentOperationAuditWriter? _operationAudit;

    public DeployLowCostArtifactSetCommandHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore,
        IEdgeCommandStore edgeCommandStore,
        IOptions<LowCostControllerCapacityOptions> capacity,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        ProductionInventoryReadinessGuard inventoryReadiness)
    {
        _deploymentStore = deploymentStore;
        _releaseStore = releaseStore;
        _edgeCommandStore = edgeCommandStore;
        _capacity = capacity.Value;
        _wakeUpPublisher = wakeUpPublisher;
        _inventoryReadiness = inventoryReadiness;
    }

    public DeployLowCostArtifactSetCommandHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore,
        IEdgeCommandStore edgeCommandStore,
        IOptions<LowCostControllerCapacityOptions> capacity,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        ProductionInventoryReadinessGuard inventoryReadiness,
        DeploymentValidationService deploymentValidation)
        : this(deploymentStore, releaseStore, edgeCommandStore, capacity, wakeUpPublisher, inventoryReadiness)
    {
        _deploymentValidation = deploymentValidation;
    }

    public DeployLowCostArtifactSetCommandHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore,
        IEdgeCommandStore edgeCommandStore,
        IOptions<LowCostControllerCapacityOptions> capacity,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        ProductionInventoryReadinessGuard inventoryReadiness,
        DeploymentValidationService deploymentValidation,
        IConfigurationDeploymentPreviewService deploymentPreview)
        : this(deploymentStore, releaseStore, edgeCommandStore, capacity, wakeUpPublisher,
            inventoryReadiness, deploymentValidation)
    {
        _deploymentPreview = deploymentPreview;
    }

    public DeployLowCostArtifactSetCommandHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore,
        IEdgeCommandStore edgeCommandStore,
        IOptions<LowCostControllerCapacityOptions> capacity,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        ProductionInventoryReadinessGuard inventoryReadiness,
        DeploymentValidationService deploymentValidation,
        IConfigurationDeploymentPreviewService deploymentPreview,
        DeploymentOperationAuditWriter operationAudit)
        : this(deploymentStore, releaseStore, edgeCommandStore, capacity, wakeUpPublisher,
            inventoryReadiness, deploymentValidation, deploymentPreview)
    {
        _operationAudit = operationAudit;
    }

    public async Task<ApiResult<ControllerArtifactSetDeploymentResult>> HandleAsync(
        DeployLowCostArtifactSetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty ||
            command.ConfigurationReleaseId == Guid.Empty ||
            command.KioskExecutionEndpointId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 200)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Kiosk, configuration release, and execution endpoint are required.", 400);
        }

        var reason = command.Reason?.Trim();
        if (reason is null or { Length: < 3 or > 500 })
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Deployment reason is required and must be between 3 and 500 characters.", 400);
        }

        if (command.Selections.Count == 0)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("At least one artifact selection is required.", 400);
        }

        if (command.Selections.Any(selection => selection.ExecutionRouteId == Guid.Empty || selection.RobotProgramId == Guid.Empty) ||
            command.Selections.GroupBy(selection => (selection.ExecutionRouteId, selection.RobotProgramId)).Any(group => group.Count() > 1))
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(
                "Low-cost selections require unique route and robot-program pairs.", 400);
        }

        var release = await _releaseStore.GetPublishedReleaseForDeploymentAsync(
            command.ConfigurationReleaseId,
            cancellationToken);
        if (release is null)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Published configuration release not found.", 404);
        }

        var endpoint = await _deploymentStore.GetEndpointForDeploymentAsync(
            command.KioskExecutionEndpointId,
            cancellationToken);
        if (endpoint is null)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Kiosk execution endpoint not found.", 404);
        }

        if (endpoint.KioskId != command.KioskId)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Execution endpoint does not belong to the target kiosk.", 400);
        }

        if (endpoint.Kiosk.OrganizationId != release.OrganizationId)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Configuration release does not belong to the endpoint organization.", 400);
        }

        if (endpoint.ControllerId is null)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Low-cost endpoint controller identity is missing.", 400);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseDeploy, command.UserContext, endpoint.Kiosk.OrganizationId, endpoint.Kiosk.StoreId, endpoint.KioskId))
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Access denied.", 403);
        }

        var expectedValidationChecksum = _deploymentPreview is null
            ? "legacy"
            : command.DeploymentPreviewChecksum.Trim();
        var idempotentResult = await GetIdempotentResultAsync(
            command,
            expectedValidationChecksum,
            cancellationToken);
        if (idempotentResult is not null)
        {
            await TryPublishWakeUpAsync(idempotentResult, cancellationToken);
            return idempotentResult;
        }

        if (release.Status != ConfigurationReleaseStatus.Published &&
            !(command.IsRollback && release.Status == ConfigurationReleaseStatus.Retired))
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(
                "Only a published configuration release can be deployed; retired releases are available only through rollback.",
                400);
        }

        ConfigurationDeploymentEndpointPreview? endpointPreview = null;
        if (_deploymentPreview is not null)
        {
            var selections = command.Selections.Select(item => new DeploymentPreviewSelection(
                item.ExecutionRouteId, item.RobotProgramId)).ToArray();
            var previewResult = await _deploymentPreview.HandleAsync(
                command.UserContext, command.KioskId, release.Id, endpoint.Id, selections, cancellationToken,
                allowRetiredRelease: command.IsRollback);
            if (!previewResult.Succeeded || previewResult.Data is null)
                return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(
                    previewResult.Message ?? "Deployment preview could not be rebuilt.",
                    previewResult.StatusCode);
            endpointPreview = previewResult.Data.Endpoints.SingleOrDefault(item => item.KioskExecutionEndpointId == endpoint.Id);
            if (endpointPreview is null || !endpointPreview.IsEligible)
                return ApiResult<ControllerArtifactSetDeploymentResult>
                    .Fail("Deployment is no longer eligible. Preview the deployment again.", 409)
                    .AddDetail("DeploymentPreview", (object?)endpointPreview ?? previewResult.Data);
            if (command.IsRollback && string.IsNullOrWhiteSpace(command.DeploymentPreviewChecksum))
                expectedValidationChecksum = endpointPreview.DeploymentChecksum;
            else if (!string.Equals(endpointPreview.DeploymentChecksum,
                         command.DeploymentPreviewChecksum.Trim(), StringComparison.Ordinal))
                return ApiResult<ControllerArtifactSetDeploymentResult>
                    .Fail("Deployment preview is missing or stale. Preview the deployment again.", 409)
                    .AddDetail("DeploymentPreview", endpointPreview);
        }

        var validationReport = endpointPreview?.Validation ?? _deploymentValidation?.Build(release, endpoint);
        try
        {
            if (validationReport is not null)
            {
                if (endpointPreview is null)
                    DeploymentValidationService.ValidateAcknowledgement(validationReport,
                        command.DeploymentPreviewChecksum,
                        command.AcknowledgeRemainingRisk || command.IsRollback);
                else if (validationReport.RequiresAcknowledgement &&
                         !command.AcknowledgeRemainingRisk && !command.IsRollback)
                    throw new DomainRuleException(
                        "Authorized organization acknowledgement is required for the remaining deployment risk.");
            }
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(ex.Message, 409)
                .AddDetail("DeploymentValidation", validationReport!);
        }

        var now = DateTimeOffset.UtcNow;
        var commandExpiryAt = command.CommandExpiryAt ?? now.AddMinutes(30);
        if (commandExpiryAt <= now)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Command expiry must be later than the deployment request time.", 400);
        }


        var readiness = await _inventoryReadiness.EvaluateDeployAsync(
            release,
            command.KioskId,
            command.Selections.Select(selection => selection.ExecutionRouteId).Distinct().ToArray(),
            cancellationToken);
        if (readiness.IsBlocked)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>
                .Fail("Artifact-set deployment blocked because kiosk inventory is not ready.", 409)
                .AddDetail("InventoryReadiness", readiness.Results);
        }

        var result = await _deploymentStore.ExecuteDeploymentCreationAsync(
            endpoint.ControllerId.Value,
            async ct =>
            {
                var existing = await _deploymentStore.GetControllerDeploymentByIdempotencyKeyAsync(
                    command.KioskExecutionEndpointId, command.IdempotencyKey.Trim(), ct);
                if (existing is not null)
                {
                    return await BuildIdempotentResultAsync(
                        existing,
                        command,
                        expectedValidationChecksum,
                        ct);
                }

                if (await _deploymentStore.HasPendingControllerArtifactSetDeploymentAsync(endpoint.ControllerId.Value, ct))
                    return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Controller already has a pending or installed artifact-set deployment.", 409);

                var activeSetVersion = await _deploymentStore.GetNextControllerActiveSetVersionAsync(endpoint.ControllerId.Value, ct);
                try
                {
                    var items = DeploymentCommandFactory.MaterializeLowCostItems(endpoint, release, command.Selections);

                    var deployment = ControllerArtifactSetDeployment.CreatePending(
                        endpoint.KioskId,
                        endpoint.Kiosk.OrganizationId,
                        endpoint.Id,
                        endpoint.ControllerId!.Value,
                        release.Id,
                        release.ReleaseChecksum!,
                        activeSetVersion,
                        command.IdempotencyKey.Trim(),
                        _capacity.MaxArtifactCount,
                        _capacity.MaxArtifactStorageBytes,
                        command.UserContext.AccountId,
                        now,
                        items,
                        expectedValidationChecksum,
                        validationReport?.RiskLevel ?? "Legacy",
                        JsonSerializer.Serialize(validationReport?.WarningCodes ?? []),
                        command.UserContext.AccountId,
                        now);

                    var edgeCommand = EdgeCommand.Create(
                        EdgeCommandType.DeployConfiguration,
                        deployment.KioskId,
                        deployment.KioskExecutionEndpointId,
                        DeploymentCommandFactory.BuildLowCostPayload(
                            deployment, command.RollbackTargetDeploymentId, command.CommandExpiryAt),
                        now,
                        commandExpiryAt: commandExpiryAt,
                        deploymentId: deployment.Id,
                        deploymentKind: DeploymentCommandTargetKind.LowCostArtifactSet,
                        rollbackTargetDeploymentId: command.RollbackTargetDeploymentId,
                        requestedCommandExpiryAt: command.CommandExpiryAt);

                    edgeCommand.CreatedByAccountId = command.UserContext.AccountId;

                    await _deploymentStore.AddControllerArtifactSetDeploymentAsync(deployment, ct);
                    await _edgeCommandStore.AddAsync(edgeCommand, ct);
                    if (_operationAudit is not null)
                    {
                        await _operationAudit.WriteRequestedAsync(
                            command.UserContext,
                            command.IsRollback ? ScopeRoleSets.ReleaseRollback : ScopeRoleSets.ReleaseDeploy,
                            command.IsRollback ? "ConfigurationRollbackRequested" : "ConfigurationDeploymentRequested",
                            reason,
                            endpoint.Kiosk.OrganizationId,
                            endpoint.Kiosk.StoreId,
                            deployment.KioskId,
                            deployment.KioskExecutionEndpointId,
                            deployment.Id,
                            edgeCommand.Id,
                            deployment.SourceConfigurationReleaseId,
                            deployment.ReleaseChecksum,
                            endpoint.ActiveArtifactSetDeploymentId,
                            command.RollbackTargetDeploymentId,
                            now,
                            ct);
                    }
                    await _deploymentStore.SaveChangesAsync(ct);

                    return ApiResult<ControllerArtifactSetDeploymentResult>.Success(
                        ControllerArtifactSetDeploymentResult.FromEntity(deployment, edgeCommand.Id),
                        "Controller artifact-set deployment requested successfully.",
                        201);
                }
                catch (DomainRuleException ex)
                {
                    return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(ex.Message, 400);
                }
            },
            cancellationToken);

        await TryPublishWakeUpAsync(result, cancellationToken);

        if (result.Succeeded && readiness.HasWarnings)
        {
            result.AddDetail("InventoryReadinessWarnings", readiness.Results.Where(item => !item.IsReady).ToArray());
        }

        return result;
    }

    private async Task<ApiResult<ControllerArtifactSetDeploymentResult>?> GetIdempotentResultAsync(
        DeployLowCostArtifactSetCommand command,
        string expectedValidationChecksum,
        CancellationToken cancellationToken)
    {
        var existing = await _deploymentStore.GetControllerDeploymentByIdempotencyKeyAsync(
            command.KioskExecutionEndpointId,
            command.IdempotencyKey.Trim(),
            cancellationToken);
        return existing is null
            ? null
            : await BuildIdempotentResultAsync(
                existing, command, expectedValidationChecksum, cancellationToken);
    }

    private async Task<ApiResult<ControllerArtifactSetDeploymentResult>> BuildIdempotentResultAsync(
        ControllerArtifactSetDeployment existing,
        DeployLowCostArtifactSetCommand command,
        string expectedValidationChecksum,
        CancellationToken cancellationToken)
    {
        var existingCommand = await _edgeCommandStore.GetByDeploymentIdAsync(existing.Id, cancellationToken);
        if (existing.SourceConfigurationReleaseId != command.ConfigurationReleaseId ||
            ((!command.IsRollback || !string.IsNullOrWhiteSpace(expectedValidationChecksum)) &&
             existing.ValidationReportChecksum != expectedValidationChecksum) ||
            !DeploymentCommandFactory.LowCostSelectionsMatch(existing, command.Selections) ||
            existingCommand is null ||
            existingCommand.RollbackTargetDeploymentId != command.RollbackTargetDeploymentId ||
            existingCommand.RequestedCommandExpiryAt != command.CommandExpiryAt)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(
                "Idempotency key was already used for a different deployment request.", 409);
        }

        return ApiResult<ControllerArtifactSetDeploymentResult>.Success(
            ControllerArtifactSetDeploymentResult.FromEntity(existing, existingCommand.Id),
            "Existing controller artifact-set deployment returned for idempotent retry.");
    }

    private Task TryPublishWakeUpAsync(
        ApiResult<ControllerArtifactSetDeploymentResult> result,
        CancellationToken cancellationToken) =>
        DeploymentCommandWakeUp.TryPublishAsync(
            result, item => item.EdgeCommandId, item => item.KioskExecutionEndpointId,
            _wakeUpPublisher, cancellationToken);

}
