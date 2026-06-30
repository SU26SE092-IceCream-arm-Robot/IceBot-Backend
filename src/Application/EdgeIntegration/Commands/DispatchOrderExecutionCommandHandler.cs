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
    private readonly IEdgeCommandWakeUpPublisher _wakeUpPublisher;

    public DispatchOrderExecutionCommandHandler(
        IOrderExecutionDispatchStore store,
        IOptions<OrderExecutionDispatchOptions> options,
        IEdgeCommandWakeUpPublisher wakeUpPublisher)
    {
        _store = store;
        _options = options.Value;
        _wakeUpPublisher = wakeUpPublisher;
    }

    public async Task<ApiResult<OrderExecutionDispatchResult>> HandleAsync(
        DispatchOrderExecutionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order execution dispatch is disabled.", 503);
        }

        if (command.OrderId == Guid.Empty || command.DispatchAttemptNo <= 0)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order and positive dispatch attempt are required.", 400);
        }

        var result = await _store.ExecuteSerializedAsync(
            command.OrderId,
            ct => DispatchLockedAsync(command, ct),
            cancellationToken);
        await PublishWakeUpAsync(result, cancellationToken);
        return result;
    }

    public async Task<ApiResult<OrderExecutionDispatchResult>> HandleRedispatchAsync(
        Guid orderId,
        Guid requestedByAccountId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order execution dispatch is disabled.", 503);
        }

        if (orderId == Guid.Empty || requestedByAccountId == Guid.Empty || string.IsNullOrWhiteSpace(reason))
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Order, requesting operator, and redispatch reason are required.", 400);
        }

        var result = await _store.ExecuteSerializedAsync(
            orderId,
            ct => RedispatchLockedAsync(orderId, requestedByAccountId, reason.Trim(), ct),
            cancellationToken);
        await PublishWakeUpAsync(result, cancellationToken);
        return result;
    }

    private async Task<ApiResult<OrderExecutionDispatchResult>> RedispatchLockedAsync(
        Guid orderId,
        Guid requestedByAccountId,
        string reason,
        CancellationToken cancellationToken)
    {
        var latest = await _store.GetLatestCommandAsync(orderId, cancellationToken);
        if (latest is null || !latest.DispatchAttemptNo.HasValue)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("No prior execution attempt was found.", 409);
        }

        if (latest.Status is EdgeCommandStatus.PendingDelivery or EdgeCommandStatus.Delivered or EdgeCommandStatus.Accepted)
        {
            if (latest.CreatedByAccountId == requestedByAccountId)
            {
                return ApiResult<OrderExecutionDispatchResult>.Success(
                    ToResult(latest, ReadConfigurationReleaseId(latest.PayloadJson), existing: true),
                    "Existing operator redispatch attempt returned.");
            }

            return ApiResult<OrderExecutionDispatchResult>.Fail("The latest execution attempt is still active.", 409);
        }

        if (latest.DispatchAttemptNo.Value >= _options.MaxDispatchAttempts)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Maximum execution dispatch attempts reached.", 409);
        }

        var order = await _store.GetOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order not found.", 404);
        }

        var eligible = latest.Status == EdgeCommandStatus.DeliveryFailed ||
            (latest.Status == EdgeCommandStatus.Rejected && order.Status == OrderStatus.ExecutionRejected);
        if (!eligible)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Redispatch is allowed only after delivery failure or rejection before physical output.", 409);
        }

        return await DispatchLockedAsync(
            new DispatchOrderExecutionCommand
            {
                OrderId = orderId,
                DispatchAttemptNo = latest.DispatchAttemptNo.Value + 1
            },
            cancellationToken,
            new RedispatchContext(requestedByAccountId, reason));
    }

    private async Task<ApiResult<OrderExecutionDispatchResult>> DispatchLockedAsync(
        DispatchOrderExecutionCommand command,
        CancellationToken cancellationToken,
        RedispatchContext? redispatch = null)
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

        var canPrepareRejectedOrder = redispatch is not null && order.Status == OrderStatus.ExecutionRejected;
        if (order.PaymentStatus != PaymentStatus.Paid ||
            (order.Status != OrderStatus.ReadyForExecution && !canPrepareRejectedOrder))
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
        edgeCommand.CreatedByAccountId = redispatch?.RequestedByAccountId;

        if (redispatch is not null)
        {
            var previousStatus = order.Status;
            if (order.Status == OrderStatus.ExecutionRejected)
            {
                order.PrepareRedispatch();
            }

            await _store.AddOrderStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.Id,
                ChangedByAccountId = redispatch.RequestedByAccountId,
                FromStatus = previousStatus,
                ToStatus = order.Status,
                ChangedAt = now,
                Reason = $"Redispatch attempt {command.DispatchAttemptNo}: {redispatch.Reason}"
            }, cancellationToken);
        }

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

    private Task PublishWakeUpAsync(
        ApiResult<OrderExecutionDispatchResult> result,
        CancellationToken cancellationToken)
    {
        if (!result.Succeeded || result.Data is null)
        {
            return Task.CompletedTask;
        }

        return _wakeUpPublisher.TryPublishAsync(
            new EdgeCommandWakeUp(
                result.Data.EdgeCommandId,
                result.Data.KioskExecutionEndpointId,
                EdgeCommandType.ExecuteOrder,
                DateTimeOffset.UtcNow),
            cancellationToken);
    }

    private sealed record DispatchCandidate(
        KioskExecutionEndpoint Endpoint,
        ConfigurationRelease Release,
        ControllerArtifactSetDeployment? ActiveSet,
        IReadOnlyCollection<ResolvedRoute> Routes);

    private sealed record ResolvedRoute(
        Guid OrderItemId,
        ExecutionRoute Route,
        IReadOnlyCollection<ExecutionRouteRobotBinding> Bindings);

    private sealed record RedispatchContext(Guid RequestedByAccountId, string Reason);
}

internal static class KioskExecutionEndpointDispatchExtensions
{
    public static bool ActiveSetVersionIsUsable(this KioskExecutionEndpoint endpoint) =>
        endpoint.ActiveArtifactSetVersion is > 0 &&
        !string.IsNullOrWhiteSpace(endpoint.ActiveArtifactSetChecksum);
}
