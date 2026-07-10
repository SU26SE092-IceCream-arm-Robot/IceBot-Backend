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

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class DeployLowCostArtifactSetCommandHandler
{
    private readonly IConfigurationDeploymentStore _deploymentStore;
    private readonly IConfigurationReleaseStore _releaseStore;
    private readonly IEdgeCommandStore _edgeCommandStore;
    private readonly LowCostControllerCapacityOptions _capacity;
    private readonly IEdgeCommandWakeUpPublisher _wakeUpPublisher;
    private readonly ProductionInventoryReadinessGuard _inventoryReadiness;

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

        if (release.Status != ConfigurationReleaseStatus.Published &&
            !(command.IsRollback && release.Status == ConfigurationReleaseStatus.Retired))
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(
                "Only a published configuration release can be deployed; retired releases are available only through rollback.",
                400);
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
                    var existingCommand = await _edgeCommandStore.GetByDeploymentIdAsync(existing.Id, ct);
                    if (existing.SourceConfigurationReleaseId != command.ConfigurationReleaseId ||
                        !SelectionsMatch(existing, command.Selections) ||
                        existingCommand is null ||
                        existingCommand.RollbackTargetDeploymentId != command.RollbackTargetDeploymentId ||
                        existingCommand.RequestedCommandExpiryAt != command.CommandExpiryAt)
                        return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Idempotency key was already used for a different deployment request.", 409);
                    return ApiResult<ControllerArtifactSetDeploymentResult>.Success(
                        ControllerArtifactSetDeploymentResult.FromEntity(existing, existingCommand?.Id),
                        "Existing controller artifact-set deployment returned for idempotent retry.");
                }

                if (await _deploymentStore.HasPendingControllerArtifactSetDeploymentAsync(endpoint.ControllerId.Value, ct))
                    return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Controller already has a pending or installed artifact-set deployment.", 409);

                var activeSetVersion = await _deploymentStore.GetNextControllerActiveSetVersionAsync(endpoint.ControllerId.Value, ct);
                try
                {
            var items = MaterializeItems(endpoint, release, command.Selections);

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
                items);

            var edgeCommand = EdgeCommand.Create(
                EdgeCommandType.DeployConfiguration,
                deployment.KioskId,
                deployment.KioskExecutionEndpointId,
                BuildDeployPayload(deployment, command.RollbackTargetDeploymentId, command.CommandExpiryAt),
                now,
                commandExpiryAt: commandExpiryAt,
                deploymentId: deployment.Id,
                deploymentKind: DeploymentCommandTargetKind.LowCostArtifactSet,
                rollbackTargetDeploymentId: command.RollbackTargetDeploymentId,
                requestedCommandExpiryAt: command.CommandExpiryAt);

            edgeCommand.CreatedByAccountId = command.UserContext.AccountId;

            await _deploymentStore.AddControllerArtifactSetDeploymentAsync(deployment, ct);
            await _edgeCommandStore.AddAsync(edgeCommand, ct);
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

        if (result.Succeeded && result.Data?.EdgeCommandId is Guid edgeCommandId)
        {
            await _wakeUpPublisher.TryPublishAsync(
                new EdgeCommandWakeUp(
                    edgeCommandId,
                    result.Data.KioskExecutionEndpointId,
                    EdgeCommandType.DeployConfiguration,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }

        if (result.Succeeded && readiness.HasWarnings)
        {
            result.AddDetail("InventoryReadinessWarnings", readiness.Results.Where(item => !item.IsReady).ToArray());
        }

        return result;
    }

    private static string BuildDeployPayload(
        ControllerArtifactSetDeployment deployment,
        Guid? rollbackTargetDeploymentId,
        DateTimeOffset? requestedCommandExpiryAt)
    {
        var payload = new
        {
            DeploymentId = deployment.Id,
            deployment.KioskId,
            TargetExecutionEndpointId = deployment.KioskExecutionEndpointId,
            deployment.ControllerId,
            ConfigurationReleaseId = deployment.SourceConfigurationReleaseId,
            deployment.ReleaseChecksum,
            RollbackTargetDeploymentId = rollbackTargetDeploymentId,
            RequestedCommandExpiryAt = requestedCommandExpiryAt,
            deployment.ActiveSetVersion,
            deployment.ActiveSetChecksum,
            deployment.MaxArtifactCount,
            deployment.MaxArtifactStorageBytes,
            deployment.RequestedArtifactCount,
            deployment.RequestedArtifactStorageBytes,
            Items = deployment.Items
                .OrderBy(item => item.ExecutionRouteId)
                .ThenBy(item => item.RobotProgramId)
                .ThenBy(item => item.RunOrder)
                .ThenBy(item => item.RobotArtifactId)
                .Select(item => new
                {
                    item.ExecutionRouteId,
                    item.RobotProgramId,
                    item.RobotProgramManifestChecksum,
                    item.RobotArtifactId,
                    item.ArtifactChecksum,
                    item.StorageKey,
                    item.RuntimeTargetCode,
                    item.MachineModelCode,
                    item.DeviceId,
                    item.ContentLengthBytes,
                    item.RunOrder,
                    item.ParametersSchemaVersion,
                    item.ParametersJson
                })
                .ToArray()
        };

        return JsonSerializer.Serialize(payload);
    }

    private static bool SelectionsMatch(
        ControllerArtifactSetDeployment existing,
        IReadOnlyCollection<DeployLowCostArtifactSelection> requested)
    {
        var existingKeys = existing.Items
            .Select(item => (item.ExecutionRouteId, item.RobotProgramId))
            .Distinct()
            .OrderBy(item => item.ExecutionRouteId).ThenBy(item => item.RobotProgramId)
            .ToArray();
        var requestedKeys = requested
            .Select(item => (item.ExecutionRouteId, item.RobotProgramId))
            .Distinct()
            .OrderBy(item => item.ExecutionRouteId).ThenBy(item => item.RobotProgramId)
            .ToArray();
        return existingKeys.SequenceEqual(requestedKeys);
    }

    private static IReadOnlyCollection<ControllerArtifactSetItemSnapshot> MaterializeItems(
        Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint endpoint,
        ConfigurationRelease release,
        IReadOnlyCollection<DeployLowCostArtifactSelection> selections)
    {
        var items = new List<ControllerArtifactSetItemSnapshot>(selections.Count);
        foreach (var selection in selections)
        {
            var route = release.ExecutionRoutes.SingleOrDefault(item => item.Id == selection.ExecutionRouteId)
                ?? throw new DomainRuleException("Selected active-set route does not belong to the source release.");
            var binding = route.RobotBindings.SingleOrDefault(item => item.RobotProgramId == selection.RobotProgramId)
                ?? throw new DomainRuleException("Selected active-set program does not belong to the source route.");
            var program = binding.RobotProgram;
            if (!AppliesToKiosk(route.ProductVariant.Product.OrganizationId, route.ProductVariant.Product.StoreId,
                    route.ProductVariant.Product.KioskId, endpoint.Kiosk) ||
                !AppliesToKiosk(route.Recipe.OrganizationId, route.Recipe.StoreId, route.Recipe.KioskId, endpoint.Kiosk) ||
                !AppliesToKiosk(program.OrganizationId, program.StoreId, program.KioskId, endpoint.Kiosk))
                throw new DomainRuleException("Selected route, recipe, and robot program must apply to the target kiosk scope.");

            var programManifest = RobotProgramManifestBuilder.Parse(
                program.ProgramManifestJson
                    ?? throw new DomainRuleException("Selected robot program has no published artifact manifest."));
            foreach (var programArtifact in programManifest.Artifacts.OrderBy(item => item.RunOrder))
            {
                var artifact = programArtifact.RobotArtifact;
                if (!endpoint.SupportsRobotTarget(artifact.RuntimeTargetCode, artifact.MachineModelCode, program.DeviceId))
                    throw new DomainRuleException("Selected robot program contains an artifact that is not compatible with the controller endpoint.");

                items.Add(new ControllerArtifactSetItemSnapshot(
                    route.Id, program.Id, program.ProgramManifestChecksum!, artifact.Id, artifact.Checksum,
                    artifact.StorageKey, artifact.RuntimeTargetCode, artifact.MachineModelCode, program.DeviceId,
                    artifact.ContentLengthBytes, programArtifact.RunOrder, programArtifact.ParametersSchemaVersion,
                    programArtifact.Parameters?.ToJsonString()));
            }
        }
        return items;
    }

    private static bool AppliesToKiosk(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Domain.Tenants.Entities.Kiosk kiosk)
    {
        return (!organizationId.HasValue || organizationId == kiosk.OrganizationId) &&
            (!storeId.HasValue || storeId == kiosk.StoreId) &&
            (!kioskId.HasValue || kioskId == kiosk.Id);
    }

}
