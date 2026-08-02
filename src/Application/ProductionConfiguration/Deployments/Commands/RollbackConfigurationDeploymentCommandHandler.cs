using Domain.Devices.ExecutionEndpoints;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Releases.ReadModels;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Application.ProductionConfiguration.Releases.Results;
using Application.ProductionConfiguration.Deployments.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Devices.Catalog;

namespace Application.ProductionConfiguration.Deployments.Commands;

public interface IConfigurationDeploymentRollbackDispatcher
{
    Task<ApiResult<ConfigurationDeploymentRollbackResult>> HandleAsync(
        RollbackConfigurationDeploymentCommand command, CancellationToken cancellationToken = default);
}

public sealed class RollbackConfigurationDeploymentCommandHandler : IConfigurationDeploymentRollbackDispatcher
{
    private readonly IConfigurationDeploymentStore _store;
    private readonly DeployFullEdgeConfigurationCommandHandler _fullEdgeDeployHandler;
    private readonly DeployLowCostArtifactSetCommandHandler _lowCostDeployHandler;

    public RollbackConfigurationDeploymentCommandHandler(
        IConfigurationDeploymentStore store,
        DeployFullEdgeConfigurationCommandHandler fullEdgeDeployHandler,
        DeployLowCostArtifactSetCommandHandler lowCostDeployHandler)
    {
        _store = store;
        _fullEdgeDeployHandler = fullEdgeDeployHandler;
        _lowCostDeployHandler = lowCostDeployHandler;
    }

    public async Task<ApiResult<ConfigurationDeploymentRollbackResult>> HandleAsync(
        RollbackConfigurationDeploymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.Reason?.Trim();
        if (reason is null or { Length: < 3 or > 500 })
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail(
                "Rollback reason is required and must be between 3 and 500 characters.", 400);
        }

        if (command.ExpectedActiveDeploymentId == Guid.Empty)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail(
                "Expected active deployment id is required.", 400);
        }

        var target = await _store.GetConfigurationDeploymentAsync(command.TargetDeploymentId, cancellationToken);
        if (target is null)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Rollback target deployment not found.", 404);
        }

        if (target.KioskId != command.KioskId)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Rollback target deployment not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseRollback,
                command.UserContext, target.OrganizationId, target.StoreId, target.KioskId))
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Access denied.", 403);
        }

        if (target.Status != ConfigurationDeploymentReadStatus.Active)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Only a previously active deployment can be selected as a rollback target.", 400);
        }

        var endpoint = await _store.GetEndpointForDeploymentAsync(target.KioskExecutionEndpointId, cancellationToken);
        if (endpoint is null || endpoint.KioskId != target.KioskId)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Rollback target execution endpoint is not available.", 400);
        }

        return target.Profile switch
        {
            ConfigurationDeploymentProfile.FullEdge => await RollbackFullEdgeAsync(command, reason, target, endpoint, cancellationToken),
            ConfigurationDeploymentProfile.LowCostController => await RollbackLowCostAsync(command, reason, target, endpoint, cancellationToken),
            _ => ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Rollback target profile is not supported.", 400)
        };
    }

    private async Task<ApiResult<ConfigurationDeploymentRollbackResult>> RollbackFullEdgeAsync(
        RollbackConfigurationDeploymentCommand command,
        string reason,
        ConfigurationDeploymentReadModel target,
        Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        if (endpoint.ExecutionProfile != KioskExecutionProfile.FullEdge)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Execution endpoint profile no longer matches the Full Edge rollback target.", 409);
        }

        if (endpoint.ActiveConfigurationDeploymentId == target.Id)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("The selected deployment is already active.", 409);
        }

        if (endpoint.ActiveConfigurationDeploymentId != command.ExpectedActiveDeploymentId)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail(
                "The endpoint active deployment changed. Refresh deployment history before retrying rollback.", 409);
        }

        var result = await _fullEdgeDeployHandler.HandleAsync(
            new DeployFullEdgeConfigurationCommand
            {
                UserContext = command.UserContext,
                KioskId = target.KioskId,
                ConfigurationReleaseId = target.ConfigurationReleaseId,
                KioskExecutionEndpointId = target.KioskExecutionEndpointId,
                IdempotencyKey = command.IdempotencyKey,
                Reason = reason,
                CommandExpiryAt = command.CommandExpiryAt,
                RollbackTargetDeploymentId = target.Id
            },
            cancellationToken);

        if (!result.Succeeded || result.Data is null || !result.Data.EdgeCommandId.HasValue)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail(
                result.Message ?? "Full Edge rollback request failed.", result.StatusCode);
        }

        return ApiResult<ConfigurationDeploymentRollbackResult>.Success(
            new ConfigurationDeploymentRollbackResult
            {
                TargetDeploymentId = target.Id,
                NewDeploymentId = result.Data.Id,
                EdgeCommandId = result.Data.EdgeCommandId.Value,
                Profile = target.Profile.ToString(),
                KioskId = target.KioskId,
                KioskExecutionEndpointId = target.KioskExecutionEndpointId,
                ConfigurationReleaseId = target.ConfigurationReleaseId,
                ReleaseChecksum = target.ReleaseChecksum,
                Status = result.Data.Status
            },
            "Full Edge rollback deployment requested successfully.",
            201);
    }

    private async Task<ApiResult<ConfigurationDeploymentRollbackResult>> RollbackLowCostAsync(
        RollbackConfigurationDeploymentCommand command,
        string reason,
        ConfigurationDeploymentReadModel target,
        Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        if (endpoint.ExecutionProfile != KioskExecutionProfile.LowCostController)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Execution endpoint profile no longer matches the low-cost rollback target.", 409);
        }

        if (endpoint.ActiveArtifactSetDeploymentId == target.Id)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("The selected artifact set is already active.", 409);
        }

        if (endpoint.ActiveArtifactSetDeploymentId != command.ExpectedActiveDeploymentId)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail(
                "The endpoint active deployment changed. Refresh deployment history before retrying rollback.", 409);
        }

        var source = await _store.GetControllerArtifactSetDeploymentForRollbackAsync(target.Id, cancellationToken);
        if (source is null || source.Items.Count == 0)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail("Rollback target artifact set is incomplete.", 400);
        }

        var result = await _lowCostDeployHandler.HandleAsync(
            new DeployLowCostArtifactSetCommand
            {
                UserContext = command.UserContext,
                KioskId = target.KioskId,
                ConfigurationReleaseId = target.ConfigurationReleaseId,
                KioskExecutionEndpointId = target.KioskExecutionEndpointId,
                IdempotencyKey = command.IdempotencyKey,
                Reason = reason,
                Selections = source.Items
                    .Select(item => new DeployLowCostArtifactSelection(item.ExecutionRouteId, item.RobotProgramId))
                    .Distinct()
                    .ToArray(),
                CommandExpiryAt = command.CommandExpiryAt,
                RollbackTargetDeploymentId = target.Id
            },
            cancellationToken);

        if (!result.Succeeded || result.Data is null || !result.Data.EdgeCommandId.HasValue)
        {
            return ApiResult<ConfigurationDeploymentRollbackResult>.Fail(
                result.Message ?? "Low-cost rollback request failed.", result.StatusCode);
        }

        return ApiResult<ConfigurationDeploymentRollbackResult>.Success(
            new ConfigurationDeploymentRollbackResult
            {
                TargetDeploymentId = target.Id,
                NewDeploymentId = result.Data.Id,
                EdgeCommandId = result.Data.EdgeCommandId.Value,
                Profile = target.Profile.ToString(),
                KioskId = target.KioskId,
                KioskExecutionEndpointId = target.KioskExecutionEndpointId,
                ConfigurationReleaseId = target.ConfigurationReleaseId,
                ReleaseChecksum = target.ReleaseChecksum,
                Status = result.Data.Status
            },
            "Low-cost artifact-set rollback deployment requested successfully.",
            201);
    }
}
