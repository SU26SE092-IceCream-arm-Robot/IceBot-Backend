using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.Inventory.Support;
using Domain.Inventory.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Catalog.Entities;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;
using Npgsql;
using Domain.ProductionExecution.Enums;
using Domain.Identity.Entities;
using Domain.Operations.Entities;

namespace Infrastructure.Inventory.Persistence;

public sealed class InventoryStore : IInventoryStore
{
    private readonly IceBotDbContext _dbContext;

    public InventoryStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventorySummaryResult> GetInventorySummaryAsync(
        Guid? kioskId,
        Guid? storeId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDispenserFiltersAndScope(
            null,
            storeId,
            kioskId,
            true,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        var totalCount = await query.CountAsync(cancellationToken);

        var lowStockCount = await query.CountAsync(
            x => x.CurrentLevelStatus == Domain.Inventory.Enums.IngredientLevelStatus.Low,
            cancellationToken);

        var emptyCount = await query.CountAsync(
            x => x.CurrentLevelStatus == Domain.Inventory.Enums.IngredientLevelStatus.Unknown ||
                 (x.EstimatedQuantity.HasValue && x.EstimatedQuantity.Value <= 0),
            cancellationToken);

        var itemsList = await query
            .Include(x => x.Kiosk)
            .Include(x => x.Ingredient)
            .OrderBy(x => x.ContainerCode)
            .ToListAsync(cancellationToken);

        var items = itemsList.Select(x => new InventorySummaryItemDto
        {
            DispenserStateId = x.Id,
            KioskId = x.KioskId,
            KioskCode = x.Kiosk?.Code ?? string.Empty,
            IngredientName = x.Ingredient?.Name ?? string.Empty,
            EstimatedQuantity = x.EstimatedQuantity,
            Capacity = x.CapacityQuantity,
            Unit = x.Unit,
            Status = x.CurrentLevelStatus.ToString(),
            UpdatedAt = x.LastMeasuredAt
        }).ToList();

        return new InventorySummaryResult
        {
            TotalDispenserCount = totalCount,
            LowStockCount = lowStockCount,
            EmptyCount = emptyCount,
            Items = items
        };
    }

    public Task<IngredientDispenserState?> GetDispenserStateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.IngredientDispenserStates.IgnoreQueryFilters()
            .Include(x => x.Kiosk)
            .Include(x => x.Device)
                .ThenInclude(x => x.DeviceModel)
            .Include(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, cancellationToken);
    }

    public Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.KioskIngredientInventories.WhereNotDeleted()
            .Include(x => x.Kiosk).Include(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(Guid kioskId, Guid ingredientId, string unit, CancellationToken cancellationToken = default) =>
        _dbContext.KioskIngredientInventories.WhereNotDeleted()
            .Include(x => x.Kiosk).Include(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.KioskId == kioskId && x.IngredientId == ingredientId &&
                x.Unit == unit.Trim().ToLower(), cancellationToken);

    public Task<List<KioskIngredientInventory>> ListKioskIngredientInventoriesAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        _dbContext.KioskIngredientInventories.WhereNotDeleted().AsNoTracking()
            .Include(x => x.Ingredient)
            .Where(x => x.KioskId == kioskId)
            .OrderBy(x => x.Ingredient.Code).ThenBy(x => x.Unit)
            .ToListAsync(cancellationToken);

    public Task<InventoryRefillTask?> GetInventoryRefillTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryRefillTasks.WhereNotDeleted().FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);

    public Task<InventoryRefillTask?> GetInventoryRefillTaskByRequestKeyAsync(Guid kioskId, string requestIdempotencyKey, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryRefillTasks.WhereNotDeleted()
            .FirstOrDefaultAsync(x => x.KioskId == kioskId && x.RequestIdempotencyKey == requestIdempotencyKey, cancellationToken);

    public Task<int> CountInventoryRefillTasksAsync(
        Guid kioskId,
        Domain.Inventory.Enums.InventoryRefillTaskStatus? status,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        CancellationToken cancellationToken = default) =>
        ApplyInventoryRefillTaskFilters(kioskId, status, requestedFrom, requestedTo).CountAsync(cancellationToken);

    public Task<List<InventoryRefillTask>> ListInventoryRefillTasksAsync(
        Guid kioskId,
        Domain.Inventory.Enums.InventoryRefillTaskStatus? status,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        ApplyInventoryRefillTaskFilters(kioskId, status, requestedFrom, requestedTo)
            .AsNoTracking()
            .OrderByDescending(x => x.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<List<InventoryRefillTask>> ListActiveInventoryRefillTasksAsync(
        Guid kioskId,
        int take,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryRefillTasks.WhereNotDeleted()
            .AsNoTracking()
            .Where(task => task.KioskId == kioskId &&
                (task.Status == Domain.Inventory.Enums.InventoryRefillTaskStatus.Requested ||
                 task.Status == Domain.Inventory.Enums.InventoryRefillTaskStatus.InProgress))
            .OrderBy(task => task.Status)
            .ThenByDescending(task => task.RequestedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public Task<InventoryRefillTask?> GetActiveInventoryRefillTaskAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryRefillTasks.WhereNotDeleted().FirstOrDefaultAsync(
            x => x.KioskIngredientInventoryId == kioskIngredientInventoryId &&
                (x.Status == Domain.Inventory.Enums.InventoryRefillTaskStatus.Requested || x.Status == Domain.Inventory.Enums.InventoryRefillTaskStatus.InProgress), cancellationToken);

    public Task<List<IngredientDispenserState>> ListBoundDispenserStatesForMutationAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default) =>
        _dbContext.IngredientDispenserStates.WhereNotDeleted()
            .Where(x => x.KioskIngredientInventoryId == kioskIngredientInventoryId && x.IsActive).ToListAsync(cancellationToken);

    public Task<List<IngredientDispenserState>> ListBoundDispenserStatesAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default) =>
        _dbContext.IngredientDispenserStates.WhereNotDeleted().AsNoTracking()
            .Include(x => x.Device).Include(x => x.Ingredient)
            .Where(x => x.KioskIngredientInventoryId == kioskIngredientInventoryId && x.IsActive)
            .OrderBy(x => x.ContainerCode).ToListAsync(cancellationToken);

    public Task<Device?> GetDeviceForTopologyAsync(Guid kioskId, Guid deviceId, CancellationToken cancellationToken = default) =>
        _dbContext.Devices.WhereNotDeleted().AsNoTracking()
            .Include(device => device.Kiosk)
            .Include(device => device.DeviceType)
            .Include(device => device.DeviceModel)
            .FirstOrDefaultAsync(device => device.Id == deviceId && device.KioskId == kioskId, cancellationToken);

    public Task<Ingredient?> GetIngredientForTopologyAsync(Guid ingredientId, CancellationToken cancellationToken = default) =>
        _dbContext.Ingredients.WhereNotDeleted().AsNoTracking()
            .FirstOrDefaultAsync(ingredient => ingredient.Id == ingredientId, cancellationToken);

    public Task<Kiosk?> GetKioskForInventoryTopologyAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        _dbContext.Kiosks.WhereNotDeleted().AsNoTracking().FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);

    public Task<List<Device>> ListDevicesForInventoryTopologyAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Devices.IgnoreQueryFilters().AsNoTracking()
            .Include(device => device.DeviceType)
            .Include(device => device.DeviceModel)
            .Where(device => device.KioskId == kioskId &&
                (device.DeletedAt == null || device.IngredientDispenserStates.Any(state => state.DeletedAt == null)))
            .OrderBy(device => device.Code)
            .ToListAsync(cancellationToken);

    public Task<List<IngredientDispenserState>> ListStatesForInventoryTopologyAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.IngredientDispenserStates.IgnoreQueryFilters().AsNoTracking()
            .Include(state => state.Ingredient)
            .Include(state => state.Device)
            .Where(state => state.KioskId == kioskId && state.DeletedAt == null)
            .OrderBy(state => state.ContainerCode)
            .ToListAsync(cancellationToken);

    public Task<List<Kiosk>> ListKiosksForInventoryReadinessAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Kiosks.WhereNotDeleted().AsNoTracking()
            .Where(kiosk => kiosk.OrganizationId == organizationId)
            .OrderBy(kiosk => kiosk.Code)
            .ToListAsync(cancellationToken);

    public Task<List<RecipeItem>> ListRequiredRecipeItemsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken = default) =>
        _dbContext.RecipeItems.AsNoTracking()
            .Include(item => item.Ingredient)
            .Where(item => recipeIds.Contains(item.RecipeId) && !item.IsOptional)
            .OrderBy(item => item.RecipeId)
            .ThenBy(item => item.StepOrder)
            .ToListAsync(cancellationToken);

    public Task<List<ProductOption>> ListSupportedProductOptionsAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<string> optionCodes,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0 || optionCodes.Count == 0)
            return Task.FromResult(new List<ProductOption>());
        var normalizedCodes = optionCodes.Select(code => code.Trim().ToUpper()).Distinct().ToArray();
        return _dbContext.ProductOptions.AsNoTracking()
            .Include(option => option.OptionGroup)
            .Include(option => option.IngredientRequirements.Where(requirement => requirement.DeletedAt == null))
                .ThenInclude(requirement => requirement.Ingredient)
            .Where(option => productIds.Contains(option.OptionGroup.ProductId) &&
                normalizedCodes.Contains(option.Code.ToUpper()) && option.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> DispenserIdentityExistsAsync(
        Guid deviceId,
        string containerCode,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.IngredientDispenserStates.WhereNotDeleted().AnyAsync(state =>
            state.IsActive && state.DeviceId == deviceId && state.ContainerCode == containerCode &&
            (!excludedId.HasValue || state.Id != excludedId), cancellationToken);

    public Task<bool> HasStockMovementsAsync(Guid dispenserStateId, CancellationToken cancellationToken = default) =>
        _dbContext.StockMovements.IgnoreQueryFilters()
            .AnyAsync(movement => movement.IngredientDispenserStateId == dispenserStateId, cancellationToken);

    public Task<bool> HasActiveExecutionAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        _dbContext.OrderExecutionRecords.AnyAsync(record =>
            record.SourceCommand.KioskId == kioskId &&
            (record.Status == ProductionExecutionStatus.Accepted ||
             record.Status == ProductionExecutionStatus.Running), cancellationToken);

    public async Task AcquireDispenserMutationLockAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default)
    {
        var lockKey = InventoryConcurrency.DispenserLockKey(dispenserStateId);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    public async Task AcquireKioskIngredientInventoryMutationLockAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"inventory-balance:{kioskIngredientInventoryId:N}"}, 0))",
            cancellationToken);
    }

    public async Task AcquireInventoryRefillTaskMutationLockAsync(Guid inventoryRefillTaskId, CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"inventory-refill-task:{inventoryRefillTaskId:N}"}, 0))",
            cancellationToken);
    }

    public async Task AcquireDeviceTopologyMutationLocksAsync(
        IEnumerable<Guid> deviceIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var deviceId in deviceIds.Distinct().OrderBy(id => id))
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({InventoryConcurrency.DeviceTopologyLockKey(deviceId)}, 0))",
                cancellationToken);
        }
    }

    public Task AddDispenserStateAsync(IngredientDispenserState state, CancellationToken cancellationToken = default) =>
        _dbContext.IngredientDispenserStates.AddAsync(state, cancellationToken).AsTask();

    public Task AddKioskIngredientInventoryAsync(KioskIngredientInventory inventory, CancellationToken cancellationToken = default) =>
        _dbContext.KioskIngredientInventories.AddAsync(inventory, cancellationToken).AsTask();

    public Task AddInventoryRefillTaskAsync(InventoryRefillTask task, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryRefillTasks.AddAsync(task, cancellationToken).AsTask();

    public Task AddInventoryRefillTaskTransitionAsync(InventoryRefillTaskTransition transition, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryRefillTaskTransitions.AddAsync(transition, cancellationToken).AsTask();

    public Task<InventoryRefillTaskTransition?> GetInventoryRefillTaskTransitionByRequestKeyAsync(Guid taskId, string requestIdempotencyKey, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryRefillTaskTransitions.FirstOrDefaultAsync(
            x => x.InventoryRefillTaskId == taskId && x.RequestIdempotencyKey == requestIdempotencyKey, cancellationToken);

    public Task<Alert?> GetAlertByIdAsync(Guid alertId, CancellationToken cancellationToken = default) =>
        _dbContext.Alerts.FirstOrDefaultAsync(alert => alert.Id == alertId && alert.DeletedAt == null, cancellationToken);

    public async Task AcquireAlertMutationLockAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"inventory-alert:{alertId:N}"}, 0))",
            cancellationToken);
    }

    public Task AddInventoryReconciliationCaseAsync(InventoryReconciliationCase reconciliationCase, CancellationToken cancellationToken = default) =>
        _dbContext.InventoryReconciliationCases.AddAsync(reconciliationCase, cancellationToken).AsTask();

    public void RemoveDispenserState(IngredientDispenserState state) =>
        _dbContext.IngredientDispenserStates.Remove(state);

    public Task AddTopologyRebindRecordAsync(
        InventoryTopologyRebindRecord record,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryTopologyRebindRecords.AddAsync(record, cancellationToken).AsTask();

    public Task<List<InventoryTopologyRebindRecord>> ListTopologyRebindRecordsAsync(
        Guid dispenserStateId,
        int? take = null,
        CancellationToken cancellationToken = default) =>
        BuildTopologyRebindHistoryQuery(dispenserStateId)
            .Take(take ?? int.MaxValue)
            .ToListAsync(cancellationToken);

    private IQueryable<InventoryTopologyRebindRecord> BuildTopologyRebindHistoryQuery(Guid dispenserStateId) =>
        _dbContext.InventoryTopologyRebindRecords.AsNoTracking()
            .Where(record =>
                record.SourceDispenserStateId == dispenserStateId ||
                record.ReplacementDispenserStateId == dispenserStateId)
            .OrderByDescending(record => record.CreatedAt);

    public Task<int> CountTopologyRebindRecordsAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryTopologyRebindRecords.CountAsync(record =>
            record.SourceDispenserStateId == dispenserStateId ||
            record.ReplacementDispenserStateId == dispenserStateId,
            cancellationToken);

    public Task<List<Account>> ListAccountsForInventoryHistoryAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken = default) =>
        _dbContext.Accounts.AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .ToListAsync(cancellationToken);

    public Task<List<IngredientDispenserState>> ListActiveDispenserStatesByDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default) =>
        _dbContext.IngredientDispenserStates.WhereNotDeleted()
            .Include(state => state.Kiosk)
            .Include(state => state.Device).ThenInclude(device => device.DeviceModel)
            .Include(state => state.Ingredient)
            .Where(state => state.DeviceId == deviceId && state.IsActive)
            .OrderBy(state => state.ContainerCode)
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> ListActiveDispenserStateIdsByDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default) =>
        _dbContext.IngredientDispenserStates.WhereNotDeleted()
            .Where(state => state.DeviceId == deviceId && state.IsActive)
            .Select(state => state.Id)
            .ToListAsync(cancellationToken);

    public Task AddTopologyChangeRecordAsync(
        InventoryTopologyChangeRecord record,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryTopologyChangeRecords.AddAsync(record, cancellationToken).AsTask();

    public Task<List<InventoryTopologyChangeRecord>> ListTopologyChangeRecordsAsync(
        Guid dispenserStateId,
        int take,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryTopologyChangeRecords.AsNoTracking()
            .Where(record => record.DispenserStateId == dispenserStateId)
            .OrderByDescending(record => record.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountTopologyChangeRecordsAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventoryTopologyChangeRecords.CountAsync(
            record => record.DispenserStateId == dispenserStateId,
            cancellationToken);

    public Task<List<StockMovement>> ListStockMovementsForDispenserAsync(
        Guid dispenserStateId,
        int take,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockMovements.AsNoTracking()
            .Include(movement => movement.CreatedByAccount)
            .Where(movement => movement.IngredientDispenserStateId == dispenserStateId)
            .OrderByDescending(movement => movement.OccurredAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountStockMovementsForDispenserAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockMovements.CountAsync(
            movement => movement.IngredientDispenserStateId == dispenserStateId,
            cancellationToken);

    public Task<List<InventorySensorObservation>> ListSensorObservationsForDispenserAsync(
        Guid dispenserStateId,
        int take,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventorySensorObservations.AsNoTracking()
            .Where(observation => observation.IngredientDispenserStateId == dispenserStateId)
            .OrderByDescending(observation => observation.CloudReceivedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountSensorObservationsForDispenserAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default) =>
        _dbContext.InventorySensorObservations.CountAsync(
            observation => observation.IngredientDispenserStateId == dispenserStateId,
            cancellationToken);

    public async Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
    }

    public Task<int> CountDispenserStatesAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool? isActive,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDispenserFiltersAndScope(
            organizationId,
            storeId,
            kioskId,
            isActive,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query.CountAsync(cancellationToken);
    }

    public Task<List<IngredientDispenserState>> ListDispenserStatesAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool? isActive,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDispenserFiltersAndScope(
            organizationId,
            storeId,
            kioskId,
            isActive,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query
            .Include(x => x.Kiosk)
            .Include(x => x.Device)
            .Include(x => x.Ingredient)
            .OrderBy(x => x.ContainerCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountStockMovementsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyMovementFiltersAndScope(
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query.CountAsync(cancellationToken);
    }

    public Task<List<StockMovement>> ListStockMovementsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyMovementFiltersAndScope(
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query
            .Include(x => x.IngredientDispenserState)
            .Include(x => x.CreatedByAccount)
            .Include(x => x.Kiosk)
            .Include(x => x.Device)
            .Include(x => x.Ingredient)
            .OrderByDescending(x => x.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        const string savepointName = "inventory_try_save";
        var transaction = _dbContext.Database.CurrentTransaction;
        if (transaction?.SupportsSavepoints == true)
        {
            await transaction.CreateSavepointAsync(savepointName, cancellationToken);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction?.SupportsSavepoints == true)
            {
                await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
            }
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            if (transaction?.SupportsSavepoints == true)
            {
                await transaction.RollbackToSavepointAsync(savepointName, cancellationToken);
            }
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private IQueryable<IngredientDispenserState> ApplyDispenserFiltersAndScope(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool? isActive,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.IngredientDispenserStates.WhereNotDeleted();

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(x => x.Kiosk != null && x.Kiosk.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(x => x.Kiosk != null && x.Kiosk.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(x => x.KioskId == kioskId.Value);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgs = allowedOrganizationIds ?? Array.Empty<Guid>();
            var allowedStores = allowedStoreIds ?? Array.Empty<Guid>();
            var allowedKiosks = allowedKioskIds ?? Array.Empty<Guid>();

            query = query.Where(x =>
                (x.Kiosk != null && allowedOrgs.Contains(x.Kiosk.OrganizationId)) ||
                (x.Kiosk != null && allowedStores.Contains(x.Kiosk.StoreId)) ||
                (x.KioskId.HasValue && allowedKiosks.Contains(x.KioskId.Value)));
        }

        return query;
    }

    private IQueryable<InventoryRefillTask> ApplyInventoryRefillTaskFilters(
        Guid kioskId,
        Domain.Inventory.Enums.InventoryRefillTaskStatus? status,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo)
    {
        var query = _dbContext.InventoryRefillTasks.WhereNotDeleted().Where(x => x.KioskId == kioskId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (requestedFrom.HasValue) query = query.Where(x => x.RequestedAt >= requestedFrom.Value);
        if (requestedTo.HasValue) query = query.Where(x => x.RequestedAt < requestedTo.Value);
        return query;
    }

    private IQueryable<StockMovement> ApplyMovementFiltersAndScope(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.StockMovements.AsQueryable();

        if (organizationId.HasValue)
        {
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(x => x.KioskId == kioskId.Value);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgs = allowedOrganizationIds ?? Array.Empty<Guid>();
            var allowedStores = allowedStoreIds ?? Array.Empty<Guid>();
            var allowedKiosks = allowedKioskIds ?? Array.Empty<Guid>();

            query = query.Where(x =>
                (x.OrganizationId.HasValue && allowedOrgs.Contains(x.OrganizationId.Value)) ||
                (x.StoreId.HasValue && allowedStores.Contains(x.StoreId.Value)) ||
                (x.KioskId.HasValue && allowedKiosks.Contains(x.KioskId.Value)));
        }

        return query;
    }
}
