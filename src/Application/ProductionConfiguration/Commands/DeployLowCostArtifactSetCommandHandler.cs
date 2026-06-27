using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.ProductionConfiguration.Commands;

public sealed class DeployLowCostArtifactSetCommandHandler
{
    private readonly IProductionConfigurationStore _productionConfigurationStore;
    private readonly IEdgeCommandStore _edgeCommandStore;

    public DeployLowCostArtifactSetCommandHandler(
        IProductionConfigurationStore productionConfigurationStore,
        IEdgeCommandStore edgeCommandStore)
    {
        _productionConfigurationStore = productionConfigurationStore;
        _edgeCommandStore = edgeCommandStore;
    }

    public async Task<ApiResult<ControllerArtifactSetDeploymentResult>> HandleAsync(
        DeployLowCostArtifactSetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty ||
            command.ConfigurationReleaseId == Guid.Empty ||
            command.KioskExecutionEndpointId == Guid.Empty)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Kiosk, configuration release, and execution endpoint are required.", 400);
        }

        if (command.MaxArtifactCount <= 0 || command.MaxArtifactStorageBytes <= 0)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Controller artifact capacity limits must be positive.", 400);
        }

        if (command.Selections.Count == 0)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("At least one artifact selection is required.", 400);
        }

        var release = await _productionConfigurationStore.GetPublishedReleaseForDeploymentAsync(
            command.ConfigurationReleaseId,
            cancellationToken);
        if (release is null)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Published configuration release not found.", 404);
        }

        var endpoint = await _productionConfigurationStore.GetEndpointForDeploymentAsync(
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

        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, endpoint.Kiosk.OrganizationId, endpoint.Kiosk.StoreId, endpoint.KioskId))
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Access denied.", 403);
        }

        if (await _productionConfigurationStore.HasPendingControllerArtifactSetDeploymentAsync(endpoint.ControllerId.Value, cancellationToken))
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Controller already has a pending or installed artifact-set deployment.", 409);
        }

        var now = DateTimeOffset.UtcNow;
        var commandExpiryAt = command.CommandExpiryAt ?? now.AddMinutes(30);
        if (commandExpiryAt <= now)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail("Command expiry must be later than the deployment request time.", 400);
        }

        var activeSetVersion = await _productionConfigurationStore.GetNextControllerActiveSetVersionAsync(
            endpoint.ControllerId.Value,
            cancellationToken);

        try
        {
            var selections = command.Selections.Select(selection => new ControllerArtifactSetItemSelection(
                selection.ExecutionRouteId,
                selection.RobotProgramId,
                selection.RobotArtifactId,
                selection.RunOrder));

            var deployment = ControllerArtifactSetDeployment.CreatePending(
                endpoint,
                release,
                activeSetVersion,
                command.MaxArtifactCount,
                command.MaxArtifactStorageBytes,
                command.UserContext.AccountId,
                now,
                selections,
                command.IsRollback);

            var edgeCommand = EdgeCommand.Create(
                EdgeCommandType.DeployConfiguration,
                deployment.KioskId,
                deployment.KioskExecutionEndpointId,
                BuildDeployPayload(deployment, command.RollbackTargetDeploymentId),
                now,
                commandExpiryAt: commandExpiryAt,
                deploymentId: deployment.Id,
                deploymentKind: DeploymentCommandTargetKind.LowCostArtifactSet);

            edgeCommand.CreatedByAccountId = command.UserContext.AccountId;

            await _productionConfigurationStore.AddControllerArtifactSetDeploymentAsync(deployment, cancellationToken);
            await _edgeCommandStore.AddAsync(edgeCommand, cancellationToken);
            await _productionConfigurationStore.SaveChangesAsync(cancellationToken);

            return ApiResult<ControllerArtifactSetDeploymentResult>.Success(
                ControllerArtifactSetDeploymentResult.FromEntity(deployment, edgeCommand.Id),
                "Controller artifact-set deployment requested successfully.",
                201);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ControllerArtifactSetDeploymentResult>.Fail(ex.Message, 400);
        }
    }

    private static string BuildDeployPayload(
        ControllerArtifactSetDeployment deployment,
        Guid? rollbackTargetDeploymentId)
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
}
