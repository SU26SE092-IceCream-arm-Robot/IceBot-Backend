using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Results;
using Application.Shared.Wrappers;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionConfiguration.Entities;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;

namespace Application.EdgeIntegration.Commands;

public sealed class DispatchOrderExecutionCommandHandler
{
    private readonly IOrderExecutionDispatchStore _store;
    private readonly OrderExecutionDispatchOptions _options;

    public DispatchOrderExecutionCommandHandler(
        IOrderExecutionDispatchStore store,
        IOptions<OrderExecutionDispatchOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public Task<ApiResult<OrderExecutionDispatchResult>> HandleAsync(
        DispatchOrderExecutionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(ApiResult<OrderExecutionDispatchResult>.Fail(
                "Order execution dispatch is disabled.", 503));
        }

        if (command.OrderId == Guid.Empty || command.DispatchAttemptNo <= 0)
        {
            return Task.FromResult(ApiResult<OrderExecutionDispatchResult>.Fail(
                "Order and positive dispatch attempt are required.", 400));
        }

        return _store.ExecuteSerializedAsync(
            command.OrderId,
            ct => DispatchLockedAsync(command, ct),
            cancellationToken);
    }

    private async Task<ApiResult<OrderExecutionDispatchResult>> DispatchLockedAsync(
        DispatchOrderExecutionCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetCommandAsync(command.OrderId, command.DispatchAttemptNo, cancellationToken);
        if (existing is not null)
        {
            return ApiResult<OrderExecutionDispatchResult>.Success(
                ToResult(existing, ReadConfigurationReleaseId(existing.PayloadJson), existing: true),
                "Existing order execution command returned for idempotent dispatch retry.");
        }

        var order = await _store.GetOrderAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order not found.", 404);
        }

        if (order.PaymentStatus != PaymentStatus.Paid || order.Status != OrderStatus.ReadyForExecution)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Only paid orders ready for execution can be dispatched.", 409);
        }

        var productionItems = order.OrderItems
            .Where(item => item.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced)
            .OrderBy(item => item.Id)
            .ToArray();
        if (productionItems.Length == 0)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Order has no machine-produced items to dispatch.", 409);
        }

        if (productionItems.Any(item => item.RecipeId is null))
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Every machine-produced order item requires a recipe before dispatch.", 409);
        }

        var endpoints = await _store.ListActiveEndpointsAsync(order.KioskId, cancellationToken);
        var candidates = new List<DispatchCandidate>();
        foreach (var endpoint in endpoints)
        {
            var candidate = await TryBuildCandidateAsync(endpoint, productionItems, cancellationToken);
            if (candidate is not null)
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "No active execution endpoint configuration covers every machine-produced order item.", 409);
        }

        if (candidates.Count > 1)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Multiple active execution endpoints can execute this order; endpoint selection is ambiguous.", 409);
        }

        var selected = candidates[0];
        await _store.AcquireEndpointAdmissionLockAsync(selected.Endpoint.Id, cancellationToken);
        var activeCommandCount = await _store.CountActiveCommandsAsync(selected.Endpoint.Id, cancellationToken);
        if (activeCommandCount >= _options.MaxActiveCommandsPerEndpoint)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Execution endpoint admission queue is full.", 409);
        }

        var now = DateTimeOffset.UtcNow;
        var expiry = command.CommandExpiryAt ?? now.AddMinutes(_options.CommandExpiryMinutes);
        if (expiry <= now)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Command expiry must be later than dispatch time.", 400);
        }

        var commandId = GuidId.New();
        var payload = BuildPayload(commandId, command.DispatchAttemptNo, order, selected, productionItems, expiry);
        var edgeCommand = EdgeCommand.Create(
            EdgeCommandType.ExecuteOrder,
            order.KioskId,
            selected.Endpoint.Id,
            payload,
            now,
            order.Id,
            command.DispatchAttemptNo,
            expiry);
        edgeCommand.Id = commandId;

        await _store.AddCommandAsync(edgeCommand, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        return ApiResult<OrderExecutionDispatchResult>.Success(
            ToResult(edgeCommand, selected.Release.Id, existing: false),
            "Order execution command created successfully.",
            201);
    }

    private async Task<DispatchCandidate?> TryBuildCandidateAsync(
        KioskExecutionEndpoint endpoint,
        IReadOnlyCollection<OrderItem> productionItems,
        CancellationToken cancellationToken)
    {
        Guid? releaseId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.ActiveConfigurationReleaseId
            : endpoint.ActiveArtifactSetReleaseId;
        string? releaseChecksum = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.ActiveConfigurationReleaseChecksum
            : endpoint.ActiveArtifactSetReleaseChecksum;
        if (!releaseId.HasValue || string.IsNullOrWhiteSpace(releaseChecksum))
        {
            return null;
        }

        var release = await _store.GetReleaseAsync(releaseId.Value, cancellationToken);
        if (release is null || !string.Equals(release.ReleaseChecksum, releaseChecksum, StringComparison.Ordinal))
        {
            return null;
        }

        ControllerArtifactSetDeployment? activeSet = null;
        if (endpoint.ExecutionProfile == KioskExecutionProfile.LowCostController)
        {
            if (!endpoint.ActiveArtifactSetDeploymentId.HasValue ||
                !endpoint.ActiveSetVersionIsUsable())
            {
                return null;
            }

            activeSet = await _store.GetControllerActiveSetAsync(
                endpoint.ActiveArtifactSetDeploymentId.Value,
                cancellationToken);
            if (activeSet is null)
            {
                return null;
            }
        }

        var routes = new List<ResolvedRoute>(productionItems.Count);
        foreach (var item in productionItems)
        {
            var route = release.ExecutionRoutes
                .Where(candidate => candidate.ProductVariantId == item.ProductVariantId && candidate.RecipeId == item.RecipeId)
                .OrderBy(candidate => candidate.Priority)
                .ThenBy(candidate => candidate.RouteCode)
                .FirstOrDefault();
            if (route is null)
            {
                return null;
            }

            var bindings = route.RobotBindings.OrderBy(binding => binding.BindingOrder).ToArray();
            if (activeSet is not null)
            {
                bindings = bindings
                    .Where(binding => activeSet.Items.Any(activeItem =>
                        activeItem.ExecutionRouteId == route.Id &&
                        activeItem.RobotProgramId == binding.RobotProgramId))
                    .ToArray();
            }

            if (bindings.Length == 0)
            {
                return null;
            }

            routes.Add(new ResolvedRoute(item.Id, route, bindings));
        }

        return new DispatchCandidate(endpoint, release, activeSet, routes);
    }

    private static string BuildPayload(
        Guid commandId,
        int dispatchAttemptNo,
        Order order,
        DispatchCandidate candidate,
        IReadOnlyCollection<OrderItem> productionItems,
        DateTimeOffset commandExpiryAt)
    {
        var routeByOrderItem = candidate.Routes.ToDictionary(route => route.OrderItemId);
        return JsonSerializer.Serialize(new
        {
            CommandId = commandId,
            DispatchAttemptNo = dispatchAttemptNo,
            OrderId = order.Id,
            order.OrderNumber,
            order.KioskId,
            TargetExecutionEndpointId = candidate.Endpoint.Id,
            ExecutionProfile = candidate.Endpoint.ExecutionProfile.ToString(),
            ConfigurationReleaseId = candidate.Release.Id,
            candidate.Release.ReleaseChecksum,
            candidate.Release.ReleaseManifestSchemaVersion,
            candidate.Release.ManifestJson,
            ActiveSetVersion = candidate.ActiveSet?.ActiveSetVersion,
            ActiveSetChecksum = candidate.ActiveSet?.ActiveSetChecksum,
            CommandExpiryAt = commandExpiryAt,
            OrderLines = productionItems.Select(item =>
            {
                var selected = routeByOrderItem[item.Id];
                return new
                {
                    OrderItemId = item.Id,
                    item.ProductId,
                    item.ProductVariantId,
                    item.RecipeId,
                    item.Quantity,
                    item.ProductCodeSnapshot,
                    item.ProductVariantCodeSnapshot,
                    item.RecipeVersionSnapshot,
                    item.RecipeSnapshotSchemaVersion,
                    item.RecipeSnapshotJson,
                    item.OptionsSchemaVersion,
                    item.OptionsJson,
                    ExecutionRouteId = selected.Route.Id,
                    selected.Route.RouteCode,
                    selected.Route.RequiredCapabilitiesJson,
                    RobotPrograms = selected.Bindings.Select(binding => new
                    {
                        binding.BindingOrder,
                        binding.RequiredWorkcellCapabilityCode,
                        RobotProgramId = binding.RobotProgram.Id,
                        binding.RobotProgram.ProgramManifestSchemaVersion,
                        binding.RobotProgram.ProgramManifestChecksum,
                        Artifacts = binding.RobotProgram.RobotProgramArtifacts
                            .OrderBy(programArtifact => programArtifact.RunOrder)
                            .Select(programArtifact => new
                            {
                                programArtifact.RobotArtifactId,
                                programArtifact.RunOrder,
                                programArtifact.ParametersSchemaVersion,
                                programArtifact.ParametersJson,
                                ArtifactChecksum = programArtifact.RobotArtifact.Checksum,
                                programArtifact.RobotArtifact.RuntimeTargetCode,
                                programArtifact.RobotArtifact.MachineModelCode
                            })
                    })
                };
            })
        });
    }

    private static Guid ReadConfigurationReleaseId(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.GetProperty("ConfigurationReleaseId").GetGuid();
    }

    private static OrderExecutionDispatchResult ToResult(
        EdgeCommand command,
        Guid releaseId,
        bool existing) => new()
        {
            OrderId = command.OrderId!.Value,
            EdgeCommandId = command.Id,
            KioskExecutionEndpointId = command.TargetExecutionEndpointId,
            ConfigurationReleaseId = releaseId,
            DispatchAttemptNo = command.DispatchAttemptNo!.Value,
            CommandExpiryAt = command.CommandExpiryAt!.Value,
            Existing = existing
        };

    private sealed record DispatchCandidate(
        KioskExecutionEndpoint Endpoint,
        ConfigurationRelease Release,
        ControllerArtifactSetDeployment? ActiveSet,
        IReadOnlyCollection<ResolvedRoute> Routes);

    private sealed record ResolvedRoute(
        Guid OrderItemId,
        ExecutionRoute Route,
        IReadOnlyCollection<ExecutionRouteRobotBinding> Bindings);
}

internal static class KioskExecutionEndpointDispatchExtensions
{
    public static bool ActiveSetVersionIsUsable(this KioskExecutionEndpoint endpoint) =>
        endpoint.ActiveArtifactSetVersion is > 0 &&
        !string.IsNullOrWhiteSpace(endpoint.ActiveArtifactSetChecksum);
}
