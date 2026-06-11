using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Domain.Inventory.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
        return _dbContext.IngredientDispenserStates
            .Include(x => x.Kiosk)
            .Include(x => x.Device)
            .Include(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
    }

    public Task<int> CountDispenserStatesAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
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
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.IngredientDispenserStates.AsQueryable();

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
