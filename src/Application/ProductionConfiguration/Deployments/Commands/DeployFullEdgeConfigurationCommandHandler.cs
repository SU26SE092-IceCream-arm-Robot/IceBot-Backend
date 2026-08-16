using Application.RobotConfiguration.Storage.Abstractions;
using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Deployments.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Manifests;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Domain.RobotConfiguration.Programs.Manifests;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Deployments.Services;

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class DeployFullEdgeConfigurationCommandHandler
{
    private readonly IConfigurationDeploymentStore _deploymentStore;
    private readonly IConfigurationReleaseStore _releaseStore;
    private readonly IEdgeCommandStore _edgeCommandStore;
    private readonly IEdgeCommandWakeUpPublisher _wakeUpPublisher;
    private readonly FullEdgeReleaseBundleService _bundleService;
    private readonly IConfigurationDeploymentPreviewService _deploymentPreview;
    private readonly DeploymentOperationAuditWriter _operationAudit;

    public DeployFullEdgeConfigurationCommandHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore,
        IEdgeCommandStore edgeCommandStore,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        FullEdgeReleaseBundleService bundleService,
        IConfigurationDeploymentPreviewService deploymentPreview,
        DeploymentOperationAuditWriter operationAudit)
    {
        _deploymentStore = deploymentStore;
        _releaseStore = releaseStore;
        _edgeCommandStore = edgeCommandStore;
        _wakeUpPublisher = wakeUpPublisher;
        _bundleService = bundleService;
        _deploymentPreview = deploymentPreview;
        _operationAudit = operationAudit;
    }

    public async Task<ApiResult<KioskConfigurationDeploymentResult>> HandleAsync(
        DeployFullEdgeConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty ||
            command.ConfigurationReleaseId == Guid.Empty ||
            command.KioskExecutionEndpointId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 200)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Kiosk, configuration release, and execution endpoint are required.", 400);
        }

        var reason = command.Reason?.Trim();
        if (reason is null or { Length: < 3 or > 500 })
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Deployment reason is required and must be between 3 and 500 characters.", 400);
        }

        var release = await _releaseStore.GetPublishedReleaseForDeploymentAsync(
            command.ConfigurationReleaseId,
            cancellationToken);
        if (release is null)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Published configuration release not found.", 404);
        }

        var endpoint = await _deploymentStore.GetEndpointForDeploymentAsync(
            command.KioskExecutionEndpointId,
            cancellationToken);
        if (endpoint is null)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Kiosk execution endpoint not found.", 404);
        }

        if (endpoint.KioskId != command.KioskId)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Execution endpoint does not belong to the target kiosk.", 400);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseDeploy, command.UserContext, endpoint.Kiosk.OrganizationId, endpoint.Kiosk.StoreId, endpoint.KioskId))
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Access denied.", 403);
        }

        var expectedValidationChecksum = command.DeploymentPreviewChecksum.Trim();
        var idempotentResult = await GetIdempotentResultAsync(
            command,
            expectedValidationChecksum,
            cancellationToken);
        if (idempotentResult is not null)
        {
            await TryPublishWakeUpAsync(idempotentResult, cancellationToken);
            return idempotentResult;
        }

        try
        {
            DeploymentRuntimeCompatibilityRules.EnsureCompatibleWhenReported(
                DeploymentCommandFactory.ListFullEdgeRuntimeProfiles(release), endpoint.ReportedDevices);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(ex.Message, 409);
        }

        var previewResult = await _deploymentPreview.HandleAsync(
            command.UserContext, command.KioskId, release.Id, endpoint.Id, [], cancellationToken,
            allowRetiredRelease: command.IsRollback);
        if (!previewResult.Succeeded || previewResult.Data is null)
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(
                previewResult.Message ?? "Deployment preview could not be rebuilt.",
                previewResult.StatusCode);

        var endpointPreview = previewResult.Data.Endpoints.SingleOrDefault(item => item.KioskExecutionEndpointId == endpoint.Id);
        if (endpointPreview is null)
            return ApiResult<KioskConfigurationDeploymentResult>
                .Fail("Deployment is no longer eligible. Preview the deployment again.", 409)
                .AddDetail("DeploymentPreview", previewResult.Data);
        if (!endpointPreview.IsEligible)
            return ApiResult<KioskConfigurationDeploymentResult>
                .Fail("Deployment is no longer eligible. Preview the deployment again.", 409)
                .AddDetail("DeploymentPreview", endpointPreview);
        if (command.IsRollback && string.IsNullOrWhiteSpace(command.DeploymentPreviewChecksum))
            expectedValidationChecksum = endpointPreview.DeploymentChecksum;
        else if (!string.Equals(endpointPreview.DeploymentChecksum,
                     command.DeploymentPreviewChecksum.Trim(), StringComparison.Ordinal))
            return ApiResult<KioskConfigurationDeploymentResult>
                .Fail("Deployment preview is missing or stale. Preview the deployment again.", 409)
                .AddDetail("DeploymentPreview", endpointPreview);

        if (endpointPreview.Validation is null)
            return ApiResult<KioskConfigurationDeploymentResult>
                .Fail("Deployment preview is missing validation evidence. Preview the deployment again.", 409)
                .AddDetail("DeploymentPreview", endpointPreview);

        var validationReport = endpointPreview.Validation;
        try
        {
            if (validationReport.RequiresAcknowledgement &&
                !command.AcknowledgeRemainingRisk && !command.IsRollback)
                throw new DomainRuleException(
                    "Authorized organization acknowledgement is required for the remaining deployment risk.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(ex.Message, 409)
                .AddDetail("DeploymentValidation", validationReport);
        }

        var now = DateTimeOffset.UtcNow;
        var commandExpiryAt = command.CommandExpiryAt ?? now.AddMinutes(30);
        if (commandExpiryAt <= now)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Command expiry must be later than the deployment request time.", 400);
        }

        FullEdgeReleaseBundleDescriptor bundle;
        try
        {
            release.ValidateFullEdgeDeploymentTarget(
                endpoint,
                endpoint.FullEdgeRuntimeId ?? Guid.Empty,
                allowRetiredRelease: command.IsRollback);
            var snapshots = PublishedRobotProgramSnapshotFactory.CreateForDeployment(release);
            bundle = await _bundleService.BuildAndStoreAsync(
                release,
                snapshots,
                release.ManifestJson ?? throw new DomainRuleException("Published release manifest is missing."),
                cancellationToken);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(ex.Message, 400);
        }
        catch (ArtifactObjectNotFoundException ex)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectIntegrityException ex)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectStorageUnavailableException)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(
                "Artifact object storage is temporarily unavailable.", 503);
        }

        var result = await _deploymentStore.ExecuteDeploymentCreationAsync(
            command.KioskId,
            async ct =>
            {
                var existing = await _deploymentStore.GetFullEdgeDeploymentByIdempotencyKeyAsync(
                    command.KioskExecutionEndpointId, command.IdempotencyKey.Trim(), ct);
                if (existing is not null)
                {
                    return await BuildIdempotentResultAsync(
                        existing,
                        command,
                        expectedValidationChecksum,
                        ct);
                }

                if (await _deploymentStore.HasPendingFullEdgeDeploymentAsync(command.KioskId, ct))
                    return ApiResult<KioskConfigurationDeploymentResult>.Fail("Kiosk already has a pending or installed configuration deployment.", 409);

                var attemptNo = await _deploymentStore.GetNextFullEdgeDeploymentAttemptNoAsync(command.KioskId, release.Id, ct);
                try
                {
                    var deployment = KioskConfigurationDeployment.CreatePending(
                        endpoint.KioskId,
                        endpoint.Kiosk.OrganizationId,
                        endpoint.Id,
                        endpoint.FullEdgeRuntimeId!.Value,
                        release.Id,
                        release.ReleaseChecksum!,
                        attemptNo,
                        command.IdempotencyKey.Trim(),
                        now,
                        command.UserContext.AccountId,
                        expectedValidationChecksum,
                        validationReport.RiskLevel,
                        JsonSerializer.Serialize(validationReport.WarningCodes),
                        command.UserContext.AccountId,
                        now);

                    var edgeCommand = EdgeCommand.Create(
                        EdgeCommandType.DeployConfiguration,
                        deployment.KioskId,
                        deployment.KioskExecutionEndpointId,
                        DeploymentCommandFactory.BuildFullEdgePayload(
                            deployment,
                            release,
                            bundle,
                            command.RollbackTargetDeploymentId,
                            command.CommandExpiryAt),
                        now,
                        commandExpiryAt: commandExpiryAt,
                        deploymentId: deployment.Id,
                        deploymentKind: DeploymentCommandTargetKind.FullEdgeConfiguration,
                        rollbackTargetDeploymentId: command.RollbackTargetDeploymentId,
                        requestedCommandExpiryAt: command.CommandExpiryAt);

                    edgeCommand.CreatedByAccountId = command.UserContext.AccountId;

                    await _deploymentStore.AddFullEdgeDeploymentAsync(deployment, ct);
                    await _edgeCommandStore.AddAsync(edgeCommand, ct);
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
                        deployment.ConfigurationReleaseId,
                        deployment.ReleaseChecksum,
                        endpoint.ActiveConfigurationDeploymentId,
                        command.RollbackTargetDeploymentId,
                        now,
                        ct);
                    await _deploymentStore.SaveChangesAsync(ct);

                    return ApiResult<KioskConfigurationDeploymentResult>.Success(
                        KioskConfigurationDeploymentResult.FromEntity(deployment, edgeCommand.Id),
                        "Configuration deployment requested successfully.",
                        201);
                }
                catch (DomainRuleException ex)
                {
                    return ApiResult<KioskConfigurationDeploymentResult>.Fail(ex.Message, 400);
                }
            },
            cancellationToken);

        await TryPublishWakeUpAsync(result, cancellationToken);
        return result;
    }

    private async Task<ApiResult<KioskConfigurationDeploymentResult>?> GetIdempotentResultAsync(
        DeployFullEdgeConfigurationCommand command,
        string expectedValidationChecksum,
        CancellationToken cancellationToken)
    {
        var existing = await _deploymentStore.GetFullEdgeDeploymentByIdempotencyKeyAsync(
            command.KioskExecutionEndpointId,
            command.IdempotencyKey.Trim(),
            cancellationToken);
        return existing is null
            ? null
            : await BuildIdempotentResultAsync(
                existing, command, expectedValidationChecksum, cancellationToken);
    }

    private async Task<ApiResult<KioskConfigurationDeploymentResult>> BuildIdempotentResultAsync(
        KioskConfigurationDeployment existing,
        DeployFullEdgeConfigurationCommand command,
        string expectedValidationChecksum,
        CancellationToken cancellationToken)
    {
        var existingCommand = await _edgeCommandStore.GetByDeploymentIdAsync(existing.Id, cancellationToken);
        if (existing.ConfigurationReleaseId != command.ConfigurationReleaseId ||
            ((!command.IsRollback || !string.IsNullOrWhiteSpace(expectedValidationChecksum)) &&
             existing.ValidationReportChecksum != expectedValidationChecksum) ||
            existingCommand is null ||
            existingCommand.RollbackTargetDeploymentId != command.RollbackTargetDeploymentId ||
            existingCommand.RequestedCommandExpiryAt != command.CommandExpiryAt)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(
                "Idempotency key was already used for a different deployment request.", 409);
        }

        return ApiResult<KioskConfigurationDeploymentResult>.Success(
            KioskConfigurationDeploymentResult.FromEntity(existing, existingCommand.Id),
            "Existing configuration deployment returned for idempotent retry.");
    }

    private Task TryPublishWakeUpAsync(
        ApiResult<KioskConfigurationDeploymentResult> result,
        CancellationToken cancellationToken) =>
        DeploymentCommandWakeUp.TryPublishAsync(
            result, item => item.EdgeCommandId, item => item.KioskExecutionEndpointId,
            _wakeUpPublisher, cancellationToken);

}
