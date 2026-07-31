using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Queries;

public sealed class GetDispenserHistoryQueryHandler(IInventoryStore inventory)
{
    public async Task<PagedResult<DispenserHistoryResult>> HandleAsync(
        GetDispenserHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var state = await inventory.GetDispenserStateByIdAsync(query.DispenserStateId, cancellationToken);
        var requestedPage = Math.Max(query.PageNumber, 1);
        var requestedPageSize = Math.Clamp(query.PageSize, 1, 100);
        if (state?.Kiosk is null)
        {
            return PagedResult<DispenserHistoryResult>.Fail(
                "Dispenser state not found.", 404, requestedPage, requestedPageSize);
        }
        if (state.KioskId != query.KioskId)
        {
            return PagedResult<DispenserHistoryResult>.Fail(
                "Dispenser state not found.", 404, requestedPage, requestedPageSize);
        }
        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.InventoryView,
                query.UserContext,
                state.Kiosk.OrganizationId,
                state.Kiosk.StoreId,
                state.KioskId))
        {
            return PagedResult<DispenserHistoryResult>.Fail(
                "Access denied.", 403, requestedPage, requestedPageSize);
        }

        var pageNumber = requestedPage;
        var pageSize = requestedPageSize;
        var take = checked(pageNumber * pageSize);
        var movements = await inventory.ListStockMovementsForDispenserAsync(
            query.DispenserStateId, take, cancellationToken);
        var observations = await inventory.ListSensorObservationsForDispenserAsync(
            query.DispenserStateId, take, cancellationToken);
        var changes = await inventory.ListTopologyChangeRecordsAsync(
            query.DispenserStateId, take, cancellationToken);
        var rebinds = await inventory.ListTopologyRebindRecordsAsync(
            query.DispenserStateId, take, cancellationToken);
        var movementCount = await inventory.CountStockMovementsForDispenserAsync(
            query.DispenserStateId, cancellationToken);
        var observationCount = await inventory.CountSensorObservationsForDispenserAsync(
            query.DispenserStateId, cancellationToken);
        var changeCount = await inventory.CountTopologyChangeRecordsAsync(
            query.DispenserStateId, cancellationToken);
        var rebindCount = await inventory.CountTopologyRebindRecordsAsync(
            query.DispenserStateId, cancellationToken);
        var actorIds = movements.Select(item => item.CreatedByAccountId)
            .Concat(changes.Select(item => item.CreatedByAccountId))
            .Concat(rebinds.Select(item => item.CreatedByAccountId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var actors = (await inventory.ListAccountsForInventoryHistoryAsync(actorIds, cancellationToken))
            .ToDictionary(account => account.Id);

        var timeline = movements.Select(movement => Entry(
                movement.Id,
                "StockMovement",
                movement.MovementType,
                movement.IngredientDispenserStateId,
                null,
                movement.ReasonCode ?? movement.Notes,
                movement.Quantity,
                movement.BalanceBefore,
                movement.BalanceAfter,
                null,
                null,
                null,
                null,
                movement.Unit,
                movement.CreatedByAccountId,
                movement.CreatedByAccountId.HasValue
                    ? "Account"
                    : movement.OriginNodeId != Guid.Empty ? "ExecutionEndpoint" : "System",
                movement.CreatedByAccountId ?? (movement.OriginNodeId != Guid.Empty ? movement.OriginNodeId : null),
                movement.OccurredAt,
                actors))
            .Concat(changes.Select(change => Entry(
                change.Id,
                "TopologyChange",
                change.ChangeType.ToString(),
                change.DispenserStateId,
                null,
                change.Reason,
                null,
                null,
                null,
                change.BeforeCapacityQuantity,
                change.AfterCapacityQuantity,
                change.BeforeIsActive,
                change.AfterIsActive,
                change.AfterUnit ?? change.BeforeUnit,
                change.CreatedByAccountId,
                change.CreatedByAccountId.HasValue ? "Account" : "System",
                change.CreatedByAccountId,
                change.CreatedAt,
                actors)))
            .Concat(observations.Select(observation => Entry(
                observation.Id,
                "SensorObservation",
                observation.ObservedLevelStatus.ToString(),
                observation.IngredientDispenserStateId,
                null,
                observation.Disposition.ToString(),
                null,
                null,
                observation.DerivedEstimatedQuantity,
                null,
                null,
                null,
                null,
                null,
                null,
                "ExecutionEndpoint",
                observation.KioskExecutionEndpointId,
                observation.ObservedAt,
                actors)))
            .Concat(rebinds.Select(rebind => Entry(
                rebind.Id,
                "TopologyRebind",
                rebind.SourceDispenserStateId == query.DispenserStateId ? "ReboundOut" : "ReboundIn",
                query.DispenserStateId,
                rebind.SourceDispenserStateId == query.DispenserStateId
                    ? rebind.ReplacementDispenserStateId
                    : rebind.SourceDispenserStateId,
                rebind.Reason,
                null,
                rebind.PreviousEstimatedQuantity,
                rebind.TransferredQuantity,
                null,
                null,
                null,
                null,
                rebind.SourceDispenserStateId == query.DispenserStateId
                    ? rebind.SourceUnit
                    : rebind.ReplacementUnit,
                rebind.CreatedByAccountId,
                rebind.CreatedByAccountId.HasValue ? "Account" : "System",
                rebind.CreatedByAccountId,
                rebind.CreatedAt,
                actors)))
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.EventId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        var total = movementCount + observationCount + changeCount + rebindCount;
        return PagedResult<DispenserHistoryResult>.Success(
            timeline,
            total,
            pageNumber,
            pageSize,
            "Dispenser history retrieved.");
    }

    private static DispenserHistoryResult Entry(
        Guid id,
        string kind,
        string action,
        Guid stateId,
        Guid? relatedStateId,
        string? reason,
        decimal? delta,
        decimal? before,
        decimal? after,
        decimal? capacityBefore,
        decimal? capacityAfter,
        bool? activeBefore,
        bool? activeAfter,
        string? unit,
        Guid? actorId,
        string actorType,
        Guid? actorReferenceId,
        DateTimeOffset occurredAt,
        IReadOnlyDictionary<Guid, Domain.Identity.Entities.Account> actors)
    {
        actors.TryGetValue(actorId ?? Guid.Empty, out var actor);
        return new DispenserHistoryResult
        {
            EventId = id,
            EventKind = kind,
            Action = action,
            DispenserStateId = stateId,
            RelatedDispenserStateId = relatedStateId,
            Reason = reason,
            QuantityDelta = delta,
            QuantityBefore = before,
            QuantityAfter = after,
            CapacityBefore = capacityBefore,
            CapacityAfter = capacityAfter,
            ActiveBefore = activeBefore,
            ActiveAfter = activeAfter,
            Unit = unit,
            ActorAccountId = actorId,
            ActorType = actorType,
            ActorReferenceId = actorReferenceId,
            ActorName = actor?.FullName,
            ActorEmail = actor?.Email,
            OccurredAt = occurredAt
        };
    }
}
