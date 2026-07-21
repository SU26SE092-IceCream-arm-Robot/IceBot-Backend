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
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Programs.Manifests;
using Application.ProductionConfiguration.Routes.Support;

namespace Application.EdgeIntegration.Dispatch.Services;

internal static class OrderExecutionDispatchPlanner
{
    public static async Task<OrderExecutionDispatchCandidate?> TryBuildCandidateAsync(
        IOrderExecutionDispatchStore store,
        KioskExecutionEndpoint endpoint,
        IReadOnlyCollection<OrderItem> productionItems,
        DateTimeOffset readinessReceivedAfter,
        CancellationToken cancellationToken)
    {
        var readiness = await store.GetReadinessAsync(endpoint.Id, cancellationToken);
        if (readiness is null || readiness.Readiness != ExecutionReadinessState.Ready ||
            readiness.Activity != ExecutionActivityState.Idle || readiness.Safety != ExecutionSafetyState.Safe ||
            readiness.CloudReceivedAt < readinessReceivedAfter)
            return null;

        var availableCapabilities = readiness.Capabilities.Where(x => x.IsAvailable)
            .Select(x => x.CapabilityCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var releaseId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.ActiveConfigurationReleaseId
            : endpoint.ActiveArtifactSetReleaseId;
        var releaseChecksum = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.ActiveConfigurationReleaseChecksum
            : endpoint.ActiveArtifactSetReleaseChecksum;
        if (!releaseId.HasValue || string.IsNullOrWhiteSpace(releaseChecksum)) return null;

        var selectedOptionCapabilityCodes = productionItems
            .SelectMany(item => item.Options)
            .SelectMany(option => option.IngredientRequirements)
            .Select(requirement => requirement.RequiredWorkcellCapabilityCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!selectedOptionCapabilityCodes.IsSubsetOf(availableCapabilities))
            return null;

        var release = await store.GetReleaseAsync(releaseId.Value, cancellationToken);
        if (release is null || !string.Equals(release.ReleaseChecksum, releaseChecksum, StringComparison.Ordinal))
            return null;

        ControllerArtifactSetDeployment? activeSet = null;
        if (endpoint.ExecutionProfile == KioskExecutionProfile.LowCostController)
        {
            if (!endpoint.ActiveArtifactSetDeploymentId.HasValue ||
                endpoint.ActiveArtifactSetVersion is not > 0 ||
                string.IsNullOrWhiteSpace(endpoint.ActiveArtifactSetChecksum))
                return null;

            activeSet = await store.GetControllerActiveSetAsync(endpoint.ActiveArtifactSetDeploymentId.Value, cancellationToken);
            if (activeSet is null) return null;
        }

        var routes = new List<OrderExecutionResolvedRoute>(productionItems.Count);
        var requiredIngredientIds = productionItems
            .SelectMany(item => item.Options)
            .SelectMany(option => option.IngredientRequirements)
            .Select(requirement => requirement.IngredientId)
            .ToHashSet();
        foreach (var item in productionItems)
        {
            var route = release.ExecutionRoutes
                .Where(candidate => candidate.ProductVariantId == item.ProductVariantId && candidate.RecipeId == item.RecipeId)
                .Where(candidate => !ExecutionRouteRequiredCapabilitiesContract.HasUnverifiableRequiredVersion(
                    candidate.RequiredCapabilitiesJson))
                .OrderBy(candidate => candidate.Priority).ThenBy(candidate => candidate.RouteCode).FirstOrDefault();
            if (route is null) return null;

            if (!ProductionDefinitionIngredientReader.TryReadRequiredIngredientIds(
                    route.ProductionDefinitionJson,
                    out var recipeIngredientIds))
                return null;
            requiredIngredientIds.UnionWith(recipeIngredientIds);

            var supportedOptionCodes = route.GetSupportedOptionCodes().ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (item.Options.Any(option =>
                    option.ExecutionImpact == Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting &&
                    !supportedOptionCodes.Contains(option.CodeSnapshot)))
                return null;

            var bindings = route.RobotBindings.OrderBy(binding => binding.BindingOrder).ToArray();
            if (activeSet is not null)
            {
                bindings = bindings.Where(binding => activeSet.Items.Any(activeItem =>
                    activeItem.ExecutionRouteId == route.Id && activeItem.RobotProgramId == binding.RobotProgramId)).ToArray();
            }

            if (bindings.Length == 0 || bindings.Any(binding =>
                    !availableCapabilities.Contains(binding.RequiredWorkcellCapabilityCode)))
                return null;

            routes.Add(new OrderExecutionResolvedRoute(item.Id, route, bindings));
        }

        var readyIngredientIds = await store.ListReadyIngredientIdsAsync(
            endpoint.KioskId,
            requiredIngredientIds.ToArray(),
            cancellationToken);
        if (readyIngredientIds.Count != requiredIngredientIds.Count)
            return null;

        return new OrderExecutionDispatchCandidate(endpoint, release, activeSet, routes);
    }

    public static string BuildPayload(
        Guid commandId,
        int dispatchAttemptNo,
        Order order,
        OrderExecutionDispatchCandidate candidate,
        IReadOnlyCollection<OrderItem> productionItems,
        DateTimeOffset commandExpiryAt)
    {
        var routeByOrderItem = candidate.Routes.ToDictionary(route => route.OrderItemId);
        return ExecuteOrderCommandPayloadCodec.Serialize(new ExecuteOrderCommandPayload
        {
            CommandId = commandId,
            DispatchAttemptNo = dispatchAttemptNo,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            KioskId = order.KioskId,
            TargetExecutionEndpointId = candidate.Endpoint.Id,
            ExecutionProfile = candidate.Endpoint.ExecutionProfile.ToString(),
            ConfigurationReleaseId = candidate.Release.Id,
            ReleaseChecksum = candidate.Release.ReleaseChecksum!,
            ReleaseManifestSchemaVersion = candidate.Release.ReleaseManifestSchemaVersion,
            ManifestJson = candidate.Release.ManifestJson!,
            ActiveSetVersion = candidate.ActiveSet?.ActiveSetVersion,
            ActiveSetChecksum = candidate.ActiveSet?.ActiveSetChecksum,
            CommandExpiryAt = commandExpiryAt,
            OrderLines = productionItems.Select(item => BuildOrderLine(item, routeByOrderItem[item.Id])).ToArray()
        });
    }

    private static ExecuteOrderLinePayload BuildOrderLine(OrderItem item, OrderExecutionResolvedRoute selected) => new()
    {
        OrderItemId = item.Id,
        ProductId = item.ProductId,
        ProductVariantId = item.ProductVariantId,
        RecipeId = item.RecipeId,
        Quantity = item.Quantity,
        ProductCodeSnapshot = item.ProductCodeSnapshot,
        ProductVariantCodeSnapshot = item.ProductVariantCodeSnapshot,
        RecipeVersionSnapshot = item.RecipeVersionSnapshot,
        RecipeSnapshotSchemaVersion = item.RecipeSnapshotSchemaVersion,
        RecipeSnapshotJson = item.RecipeSnapshotJson,
        SelectedOptions = item.Options
            .OrderBy(option => option.OptionGroupCodeSnapshot)
            .ThenBy(option => option.CodeSnapshot)
            .Select(option => new ExecuteOrderLineOptionPayload
            {
                ProductOptionId = option.ProductOptionId,
                OptionGroupId = option.OptionGroupId,
                OptionGroupCode = option.OptionGroupCodeSnapshot,
                Code = option.CodeSnapshot,
                Name = option.NameSnapshot,
                UnitPriceDelta = option.UnitPriceDelta,
                IngredientRequirements = option.IngredientRequirements
                    .OrderBy(requirement => requirement.IngredientId)
                    .Select(requirement => new ExecuteOrderOptionIngredientRequirementPayload
                    {
                        IngredientId = requirement.IngredientId,
                        IngredientCode = requirement.IngredientCodeSnapshot,
                        IngredientName = requirement.IngredientNameSnapshot,
                        QuantityPerOption = requirement.QuantityPerOption,
                        Unit = requirement.Unit,
                        RequiredWorkcellCapabilityCode = requirement.RequiredWorkcellCapabilityCode
                    }).ToArray()
            }).ToArray(),
        ExecutionRouteId = selected.Route.Id,
        RouteCode = selected.Route.RouteCode,
        RequiredCapabilitiesJson = selected.Route.RequiredCapabilitiesJson,
        ProductionDefinitionChecksum = selected.Route.ProductionDefinitionChecksum,
        RobotPrograms = selected.Bindings.Select(binding => new ExecuteOrderRobotProgramPayload
        {
            BindingOrder = binding.BindingOrder,
            RequiredWorkcellCapabilityCode = binding.RequiredWorkcellCapabilityCode,
            RobotProgramId = binding.RobotProgram.Id,
            ProgramManifestSchemaVersion = binding.RobotProgram.ProgramManifestSchemaVersion,
            ProgramManifestChecksum = binding.RobotProgram.ProgramManifestChecksum!,
            Artifacts = RobotProgramManifestBuilder.Parse(binding.RobotProgram.ProgramManifestJson
                    ?? throw new DomainRuleException("Published robot program manifest is missing."))
                .Artifacts.OrderBy(artifact => artifact.RunOrder)
                .Select(artifact => new ExecuteOrderArtifactPayload
                {
                    RobotArtifactId = artifact.RobotArtifact.Id,
                    RunOrder = artifact.RunOrder,
                    ParametersSchemaVersion = artifact.ParametersSchemaVersion,
                    ParametersJson = artifact.Parameters?.ToJsonString(),
                    ArtifactChecksum = artifact.RobotArtifact.Checksum,
                    RuntimeTargetCode = artifact.RobotArtifact.RuntimeTargetCode,
                    MachineModelCode = artifact.RobotArtifact.MachineModelCode,
                    TechnicalContractId = artifact.RobotArtifact.TechnicalContractId,
                    TechnicalContractChecksum = artifact.RobotArtifact.TechnicalContractChecksum,
                    RequiredOptionCode = artifact.RequiredOptionCode
                }).ToArray()
        }).ToArray()
    };
}

internal sealed record OrderExecutionDispatchCandidate(
    KioskExecutionEndpoint Endpoint,
    ConfigurationRelease Release,
    ControllerArtifactSetDeployment? ActiveSet,
    IReadOnlyCollection<OrderExecutionResolvedRoute> Routes);

internal sealed record OrderExecutionResolvedRoute(
    Guid OrderItemId,
    ExecutionRoute Route,
    IReadOnlyCollection<ExecutionRouteRobotBinding> Bindings);
