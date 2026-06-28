using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.ProductionConfiguration.Commands;

public sealed class DeployFullEdgeConfigurationCommandHandler
{
    private readonly IProductionConfigurationStore _productionConfigurationStore;
    private readonly IEdgeCommandStore _edgeCommandStore;

    public DeployFullEdgeConfigurationCommandHandler(
        IProductionConfigurationStore productionConfigurationStore,
        IEdgeCommandStore edgeCommandStore)
    {
        _productionConfigurationStore = productionConfigurationStore;
        _edgeCommandStore = edgeCommandStore;
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

        var release = await _productionConfigurationStore.GetPublishedReleaseForDeploymentAsync(
            command.ConfigurationReleaseId,
            cancellationToken);
        if (release is null)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Published configuration release not found.", 404);
        }

        var endpoint = await _productionConfigurationStore.GetEndpointForDeploymentAsync(
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

        var now = DateTimeOffset.UtcNow;
        var commandExpiryAt = command.CommandExpiryAt ?? now.AddMinutes(30);
        if (commandExpiryAt <= now)
        {
            return ApiResult<KioskConfigurationDeploymentResult>.Fail("Command expiry must be later than the deployment request time.", 400);
        }

        return await _productionConfigurationStore.ExecuteDeploymentCreationAsync(
            command.KioskId,
            async ct =>
            {
                var existing = await _productionConfigurationStore.GetFullEdgeDeploymentByIdempotencyKeyAsync(
                    command.KioskExecutionEndpointId, command.IdempotencyKey.Trim(), ct);
                if (existing is not null)
                {
                    var existingCommand = await _edgeCommandStore.GetByDeploymentIdAsync(existing.Id, ct);
                    if (existing.ConfigurationReleaseId != command.ConfigurationReleaseId ||
                        existingCommand is null ||
                        ReadRollbackTarget(existingCommand.PayloadJson) != command.RollbackTargetDeploymentId)
                        return ApiResult<KioskConfigurationDeploymentResult>.Fail("Idempotency key was already used for a different deployment request.", 409);
                    return ApiResult<KioskConfigurationDeploymentResult>.Success(
                        KioskConfigurationDeploymentResult.FromEntity(existing, existingCommand?.Id),
                        "Existing configuration deployment returned for idempotent retry.");
                }

                if (await _productionConfigurationStore.HasPendingFullEdgeDeploymentAsync(command.KioskId, ct))
                    return ApiResult<KioskConfigurationDeploymentResult>.Fail("Kiosk already has a pending or installed configuration deployment.", 409);

                var attemptNo = await _productionConfigurationStore.GetNextFullEdgeDeploymentAttemptNoAsync(command.KioskId, release.Id, ct);
                try
                {
            release.ValidateFullEdgeDeploymentTarget(
                endpoint, endpoint.FullEdgeRuntimeId ?? Guid.Empty, allowRetiredRelease: command.IsRollback);
            var deployment = KioskConfigurationDeployment.CreatePending(
                endpoint.KioskId,
                endpoint.Id,
                endpoint.FullEdgeRuntimeId!.Value,
                release.Id,
                release.ReleaseChecksum!,
                attemptNo,
                command.IdempotencyKey.Trim(),
                now,
                command.UserContext.AccountId);

            var edgeCommand = EdgeCommand.Create(
                EdgeCommandType.DeployConfiguration,
                deployment.KioskId,
                deployment.KioskExecutionEndpointId,
                BuildDeployPayload(deployment, release, command.RollbackTargetDeploymentId),
                now,
                commandExpiryAt: commandExpiryAt,
                deploymentId: deployment.Id,
                deploymentKind: DeploymentCommandTargetKind.FullEdgeConfiguration);

            edgeCommand.CreatedByAccountId = command.UserContext.AccountId;

            await _productionConfigurationStore.AddFullEdgeDeploymentAsync(deployment, ct);
            await _edgeCommandStore.AddAsync(edgeCommand, ct);
            await _productionConfigurationStore.SaveChangesAsync(ct);

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
    }

    private static string BuildDeployPayload(
        KioskConfigurationDeployment deployment,
        ConfigurationRelease release,
        Guid? rollbackTargetDeploymentId)
    {
        var artifacts = release.ExecutionRoutes
            .SelectMany(route => route.RobotBindings)
            .SelectMany(binding => binding.RobotProgram.RobotProgramArtifacts)
            .GroupBy(programArtifact => programArtifact.RobotArtifactId)
            .Select(group => group.First().RobotArtifact)
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
            release.ReleaseManifestSchemaVersion,
            release.ManifestJson,
            Artifacts = artifacts
        };

        return JsonSerializer.Serialize(payload);
    }

    private static Guid? ReadRollbackTarget(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        if (!document.RootElement.TryGetProperty("RollbackTargetDeploymentId", out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        return value.GetGuid();
    }
}
