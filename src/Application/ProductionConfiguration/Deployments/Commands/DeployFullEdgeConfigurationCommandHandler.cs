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
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration.Deployments.Services;

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class DeployFullEdgeConfigurationCommandHandler
{
    private readonly IConfigurationDeploymentStore _deploymentStore;
    private readonly IConfigurationReleaseStore _releaseStore;
    private readonly IEdgeCommandStore _edgeCommandStore;
    private readonly IEdgeCommandWakeUpPublisher _wakeUpPublisher;
    private readonly ProductionInventoryReadinessGuard _inventoryReadiness;
    private readonly FullEdgeReleaseBundleService _bundleService;
    private readonly DeploymentValidationService? _deploymentValidation;

    public DeployFullEdgeConfigurationCommandHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore,
        IEdgeCommandStore edgeCommandStore,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        ProductionInventoryReadinessGuard inventoryReadiness,
        FullEdgeReleaseBundleService bundleService)
    {
        _deploymentStore = deploymentStore;
        _releaseStore = releaseStore;
        _edgeCommandStore = edgeCommandStore;
        _wakeUpPublisher = wakeUpPublisher;
        _inventoryReadiness = inventoryReadiness;
        _bundleService = bundleService;
    }

    public DeployFullEdgeConfigurationCommandHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore,
        IEdgeCommandStore edgeCommandStore,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        ProductionInventoryReadinessGuard inventoryReadiness,
        FullEdgeReleaseBundleService bundleService,
        DeploymentValidationService deploymentValidation)
        : this(deploymentStore, releaseStore, edgeCommandStore, wakeUpPublisher, inventoryReadiness, bundleService)
    {
        _deploymentValidation = deploymentValidation;
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

        var expectedValidationChecksum = _deploymentValidation is null
            ? "legacy"
            : command.ValidationReportChecksum.Trim();
        var idempotentResult = await GetIdempotentResultAsync(
            command,
            expectedValidationChecksum,
            cancellationToken);
        if (idempotentResult is not null)
        {
            await TryPublishWakeUpAsync(idempotentResult, cancellationToken);
            return idempotentResult;
        }

        var validationReport = _deploymentValidation?.Build(release, endpoint);
        try
        {
            if (validationReport is not null)
                DeploymentValidationService.ValidateAcknowledgement(validationReport,
                    command.ValidationReportChecksum, command.AcknowledgeRemainingRisk);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail(ex.Message, 409)
                .AddDetail("DeploymentValidation", validationReport!);
        }

        var now = DateTimeOffset.UtcNow;
        var commandExpiryAt = command.CommandExpiryAt ?? now.AddMinutes(30);
        if (commandExpiryAt <= now)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Command expiry must be later than the deployment request time.", 400);
        }

        var readiness = await _inventoryReadiness.EvaluateDeployAsync(
            release,
            command.KioskId,
            cancellationToken: cancellationToken);
        if (readiness.IsBlocked)
        {
            return ApiResult<KioskConfigurationDeploymentResult>
                .Fail("Configuration deployment blocked because kiosk inventory is not ready.", 409)
                .AddDetail("InventoryReadiness", readiness.Results);
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
                        validationReport?.Checksum ?? "legacy",
                        validationReport?.RiskLevel ?? "Legacy",
                        JsonSerializer.Serialize(validationReport?.WarningCodes ?? []),
                        command.UserContext.AccountId,
                        now);

                    var edgeCommand = EdgeCommand.Create(
                        EdgeCommandType.DeployConfiguration,
                        deployment.KioskId,
                        deployment.KioskExecutionEndpointId,
                        BuildDeployPayload(
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

        if (result.Succeeded && readiness.HasWarnings)
        {
            result.AddDetail("InventoryReadinessWarnings", readiness.Results.Where(item => !item.IsReady).ToArray());
        }

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
            existing.ValidationReportChecksum != expectedValidationChecksum ||
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
        result.Succeeded && result.Data?.EdgeCommandId is Guid edgeCommandId
            ? _wakeUpPublisher.TryPublishAsync(
                new EdgeCommandWakeUp(
                    edgeCommandId,
                    result.Data.KioskExecutionEndpointId,
                    EdgeCommandType.DeployConfiguration,
                    DateTimeOffset.UtcNow),
                cancellationToken)
            : Task.CompletedTask;

    private static string BuildDeployPayload(
        KioskConfigurationDeployment deployment,
        ConfigurationRelease release,
        FullEdgeReleaseBundleDescriptor fullEdgeBundle,
        Guid? rollbackTargetDeploymentId,
        DateTimeOffset? requestedCommandExpiryAt)
    {
        var artifacts = release.ExecutionRoutes
            .SelectMany(route => route.RobotBindings)
            .SelectMany(binding => RobotProgramManifestBuilder.Parse(
                binding.RobotProgram.ProgramManifestJson
                    ?? throw new DomainRuleException("Published robot program manifest is missing."))
                .Artifacts)
            .Select(programArtifact => programArtifact.RobotArtifact)
            .GroupBy(artifact => artifact.Id)
            .Select(group => group.First())
            .OrderBy(artifact => artifact.Id)
            .Select(artifact => new
            {
                RobotArtifactId = artifact.Id,
                StorageKey = artifact.StorageKey,
                ArtifactChecksum = artifact.Checksum,
                artifact.RuntimeTargetCode,
                artifact.MachineModelCode,
                artifact.ContentLengthBytes
            })
            .ToArray();

        var payload = new
        {
            DeploymentId = deployment.Id,
            deployment.AttemptNo,
            deployment.KioskId,
            TargetExecutionEndpointId = deployment.KioskExecutionEndpointId,
            deployment.ConfigurationReleaseId,
            deployment.ReleaseChecksum,
            RollbackTargetDeploymentId = rollbackTargetDeploymentId,
            RequestedCommandExpiryAt = requestedCommandExpiryAt,
            release.ReleaseManifestSchemaVersion,
            release.ManifestJson,
            FullEdgeBundle = fullEdgeBundle,
            Artifacts = artifacts
        };

        return JsonSerializer.Serialize(payload);
    }

}
