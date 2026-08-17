using Domain.Devices.ExecutionEndpoints;
using Application.Orders.Abstractions;
using Domain.Devices.Catalog;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionExecution.Projections;
using Domain.SalesCatalog.Entities;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using Application.SalesCatalog.ReadModels;
using Application.Orders.PlaceOrder.ReadModels;
using Application.ProductionConfiguration.Routes.Support;
using Application.Orders.Admission;
using Application.Tenants.Kiosks.Rules;

namespace Infrastructure.Orders.Persistence;

public sealed partial class OrderStore : IOrderStore
{
    private readonly IceBotDbContext _dbContext;

    public OrderStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.WhereNotDeleted()
            .Include(kiosk => kiosk.Store)
            .Include(kiosk => kiosk.Organization)
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<Domain.Devices.Connectivity.KioskConnectivityProjection?> GetKioskConnectivityAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.KioskConnectivityProjections.AsNoTracking()
            .FirstOrDefaultAsync(connectivity => connectivity.KioskId == kioskId, cancellationToken);

    public Task AcquireKioskOperationalLockAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({KioskOperationalConcurrency.LockKey(kioskId)}, 0));",
            cancellationToken);

    public Task<bool> HasActiveCustomerSessionAsync(
        Guid kioskId,
        DateTimeOffset observedAt,
        Guid? excludingOrderId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Orders.WhereNotDeleted()
            .AnyAsync(
                KioskCustomerSessionAdmission.BuildActiveSessionPredicate(
                    kioskId, observedAt, excludingOrderId),
                cancellationToken);

    public Task<MenuItem?> GetMenuItemForKioskAsync(
        Guid menuItemId,
        Guid? organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItems
            .Include(menuItem => menuItem.Menu)
            .Include(menuItem => menuItem.Product)
            .Include(menuItem => menuItem.ProductVariant)
            .Include(menuItem => menuItem.Recipe)
                .ThenInclude(recipe => recipe!.RecipeItems)
                    .ThenInclude(item => item.Ingredient)
            .FirstOrDefaultAsync(menuItem =>
                menuItem.Id == menuItemId &&
                menuItem.Product.DeletedAt == null &&
                menuItem.Menu.OrganizationId == organizationId &&
                (!menuItem.Menu.StoreId.HasValue || menuItem.Menu.StoreId == storeId) &&
                (!menuItem.Menu.KioskId.HasValue || menuItem.Menu.KioskId == kioskId) &&
                menuItem.Product.OrganizationId == organizationId &&
                (!menuItem.Product.StoreId.HasValue || menuItem.Product.StoreId == storeId) &&
                (!menuItem.Product.KioskId.HasValue || menuItem.Product.KioskId == kioskId),
                cancellationToken);
    }

    public Task<List<MenuItemProductOptionReadModel>> ListMenuItemProductOptionsAsync(
        Guid menuItemId,
        CancellationToken cancellationToken = default)
    {
        return (from membership in _dbContext.MenuItemProductOptions.AsNoTracking()
                join option in _dbContext.ProductOptions.AsNoTracking() on membership.ProductOptionId equals option.Id
                join optionGroup in _dbContext.OptionGroups.AsNoTracking() on option.OptionGroupId equals optionGroup.Id
                where membership.MenuItemId == menuItemId && membership.DeletedAt == null &&
                      option.DeletedAt == null && optionGroup.IsActive
                select new MenuItemProductOptionReadModel(
                    membership.MenuItemId,
                    option.Id,
                    optionGroup.Id,
                    optionGroup.Code,
                    optionGroup.Name,
                    optionGroup.SelectionType,
                    optionGroup.MinSelections,
                    optionGroup.MaxSelections,
                    optionGroup.IsRequired,
                    option.Code,
                    option.Name,
                    option.Description,
                    option.PriceDelta,
                    option.ExecutionImpact,
                    option.IsAvailable,
                    !_dbContext.ProductOptionIngredientRequirements.Any(requirement =>
                        requirement.ProductOptionId == option.Id && !requirement.Ingredient.IsActive),
                    option.IsDefault,
                    option.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public Task<List<MenuItemOptionGroupReadModel>> ListMenuItemOptionGroupsAsync(
        Guid menuItemId,
        CancellationToken cancellationToken = default)
    {
        return (from menuItem in _dbContext.MenuItems.AsNoTracking()
                join optionGroup in _dbContext.OptionGroups.AsNoTracking()
                    on menuItem.ProductId equals optionGroup.ProductId
                where menuItem.Id == menuItemId && optionGroup.IsActive
                select new MenuItemOptionGroupReadModel(
                    menuItem.Id,
                    optionGroup.Id,
                    optionGroup.Code,
                    optionGroup.Name,
                    optionGroup.SelectionType,
                    optionGroup.MinSelections,
                    optionGroup.MaxSelections,
                    optionGroup.IsRequired))
            .ToListAsync(cancellationToken);
    }

    public Task<List<ProductOptionIngredientRequirementReadModel>> ListProductOptionIngredientRequirementsAsync(
        IReadOnlyCollection<Guid> productOptionIds,
        CancellationToken cancellationToken = default)
    {
        if (productOptionIds.Count == 0)
        {
            return Task.FromResult(new List<ProductOptionIngredientRequirementReadModel>());
        }

        return (from requirement in _dbContext.ProductOptionIngredientRequirements.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking() on requirement.IngredientId equals ingredient.Id
                where productOptionIds.Contains(requirement.ProductOptionId) && requirement.DeletedAt == null
                select new ProductOptionIngredientRequirementReadModel(
                    requirement.ProductOptionId,
                    ingredient.Id,
                    ingredient.Code,
                    ingredient.Name,
                    requirement.Quantity,
                    requirement.Unit,
                    requirement.RequiredWorkcellCapabilityCode,
                    ingredient.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ActiveProductionRouteOptionPolicy?> GetActiveProductionRouteOptionPolicyAsync(
        Guid kioskId,
        Guid productVariantId,
        Guid recipeId,
        DateTimeOffset readinessReceivedAfter,
        CancellationToken cancellationToken = default)
    {
        var routes = await _dbContext.ExecutionEndpointReadinessProjections
            .AsNoTracking()
            .Where(readiness =>
                readiness.KioskId == kioskId && readiness.Readiness == ExecutionReadinessState.Ready &&
                readiness.Safety == ExecutionSafetyState.Safe &&
                readiness.CloudReceivedAt >= readinessReceivedAfter &&
                readiness.KioskExecutionEndpoint.Status == KioskExecutionEndpointStatus.Active &&
                _dbContext.ConfigurationReleases.WhereNotDeleted().Any(release =>
                    release.Id == (readiness.KioskExecutionEndpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                        ? readiness.KioskExecutionEndpoint.ActiveConfigurationReleaseId
                        : readiness.KioskExecutionEndpoint.ActiveArtifactSetReleaseId) &&
                    release.Status == ConfigurationReleaseStatus.Published &&
                    release.ExecutionRoutes.Any(route => route.ProductVariantId == productVariantId && route.RecipeId == recipeId &&
                        route.RobotBindings.Any() && route.RobotBindings.All(binding =>
                            binding.RequiredWorkcellCapabilityCode == string.Empty ||
                            readiness.Capabilities.Any(capability =>
                                capability.IsAvailable && capability.CapabilityCode == binding.RequiredWorkcellCapabilityCode)))))
            .SelectMany(readiness => _dbContext.ConfigurationReleases.WhereNotDeleted()
                .Where(release => release.Id == (readiness.KioskExecutionEndpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                    ? readiness.KioskExecutionEndpoint.ActiveConfigurationReleaseId
                    : readiness.KioskExecutionEndpoint.ActiveArtifactSetReleaseId))
                .SelectMany(release => release.ExecutionRoutes.Where(route =>
                    route.ProductVariantId == productVariantId && route.RecipeId == recipeId &&
                    route.RobotBindings.Any() && route.RobotBindings.All(binding =>
                        binding.RequiredWorkcellCapabilityCode == string.Empty ||
                        readiness.Capabilities.Any(capability =>
                            capability.IsAvailable && capability.CapabilityCode == binding.RequiredWorkcellCapabilityCode)))))
            .OrderBy(route => route.Priority).ThenBy(route => route.RouteCode)
            .Select(route => new { route.Id, route.SupportedOptionCodesJson, route.RequiredCapabilitiesJson })
            .ToListAsync(cancellationToken);
        var route = routes.FirstOrDefault(candidate =>
            !ExecutionRouteRequiredCapabilitiesContract.HasUnverifiableRequiredVersion(
                candidate.RequiredCapabilitiesJson));
        return route is null ? null : new ActiveProductionRouteOptionPolicy(route.Id,
            (JsonSerializer.Deserialize<string[]>(route.SupportedOptionCodesJson) ?? [])
                .ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.WhereNotDeleted()
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Options)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task<Order?> GetManagementOrderByIdAsync(
        Guid orderId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.WhereNotDeleted()
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Options)
            .Where(order => order.Id == orderId);

        if (!isSystemAdmin)
        {
            query = query.Where(order =>
                (order.OrganizationId.HasValue && allowedOrganizationIds.Contains(order.OrganizationId.Value)) ||
                (order.StoreId.HasValue && allowedStoreIds.Contains(order.StoreId.Value)) ||
                allowedKioskIds.Contains(order.KioskId));
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountOrdersAsync(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFiltersAndScope(
            search,
            status,
            paymentStatus,
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query.CountAsync(cancellationToken);
    }

    public Task<List<Order>> ListOrdersAsync(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
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
        var query = ApplyFiltersAndScope(
            search,
            status,
            paymentStatus,
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query
            .AsNoTracking()
            .OrderByDescending(o => o.PlacedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountOrderStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories
            .AsNoTracking()
            .CountAsync(history => history.OrderId == orderId, cancellationToken);
    }

    public Task<List<OrderStatusHistory>> ListOrderStatusHistoryAsync(
        Guid orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories
            .AsNoTracking()
            .Include(history => history.ChangedByAccount)
            .Where(history => history.OrderId == orderId)
            .OrderByDescending(history => history.ChangedAt)
            .ThenByDescending(history => history.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<OrderItemStatusHistory?> GetOrderItemStatusHistoryBySourceEventIdAsync(
        Guid orderItemId,
        Guid sourceEventId,
        CancellationToken cancellationToken = default) =>
        _dbContext.OrderItemStatusHistories.AsNoTracking().FirstOrDefaultAsync(
            history => history.OrderItemId == orderItemId && history.SourceEventId == sourceEventId,
            cancellationToken);

    public async Task AcquireFulfillmentEventLockAsync(
        Guid orderItemId,
        Guid sourceEventId,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"order-item-fulfillment:{orderItemId:D}:{sourceEventId:D}";
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
    }

    public Task AcquireOrderWorkflowLockAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({Application.Orders.Support.OrderWorkflowConcurrency.OrderLockKey(orderId)}, 0));",
            cancellationToken);

    public Task<int> CountExecutionAttemptsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AsNoTracking().CountAsync(command =>
            command.CommandType == EdgeCommandType.ExecuteOrder &&
            command.OrderId == orderId,
            cancellationToken);
    }

    public Task<List<EdgeCommand>> ListExecutionAttemptsAsync(
        Guid orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AsNoTracking()
            .Where(command =>
                command.CommandType == EdgeCommandType.ExecuteOrder &&
                command.OrderId == orderId)
            .OrderByDescending(command => command.DispatchAttemptNo)
            .ThenByDescending(command => command.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<EdgeCommand?> GetExecutionAttemptAsync(
        Guid orderId,
        Guid sourceCommandId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AsNoTracking()
            .Include(command => command.DeliveryAttempts)
            .FirstOrDefaultAsync(command =>
                command.Id == sourceCommandId &&
                command.OrderId == orderId &&
                command.CommandType == EdgeCommandType.ExecuteOrder,
                cancellationToken);
    }

    public Task<List<EdgeCommand>> ListAdjacentExecutionAttemptsAsync(
        Guid orderId,
        int dispatchAttemptNo,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AsNoTracking()
            .Where(command =>
                command.CommandType == EdgeCommandType.ExecuteOrder &&
                command.OrderId == orderId &&
                (command.DispatchAttemptNo == dispatchAttemptNo - 1 ||
                 command.DispatchAttemptNo == dispatchAttemptNo + 1))
            .OrderBy(command => command.DispatchAttemptNo)
            .ToListAsync(cancellationToken);
    }

    public Task<OrderStatusHistory?> GetRedispatchHistoryAsync(
        Guid orderId,
        int dispatchAttemptNo,
        DateTimeOffset commandCreatedAt,
        Guid requestedByAccountId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories.AsNoTracking()
            .FirstOrDefaultAsync(history =>
                history.OrderId == orderId &&
                history.ChangedAt == commandCreatedAt &&
                history.ChangedByAccountId == requestedByAccountId &&
                history.Reason != null &&
                history.Reason.StartsWith($"Redispatch attempt {dispatchAttemptNo}:"),
                cancellationToken);
    }

    public Task<List<OrderExecutionRecord>> ListOrderExecutionRecordsAsync(
        IReadOnlyCollection<Guid> sourceCommandIds,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderExecutionRecords.AsNoTracking()
            .Where(record => sourceCommandIds.Contains(record.SourceCommandId))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderExecutionRecord?> GetLatestOrderExecutionRecordAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var latestCommandId = await _dbContext.EdgeCommands.AsNoTracking()
            .Where(command =>
                command.OrderId == orderId &&
                command.CommandType == EdgeCommandType.ExecuteOrder)
            .OrderByDescending(command => command.DispatchAttemptNo)
            .ThenByDescending(command => command.CreatedAt)
            .Select(command => (Guid?)command.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!latestCommandId.HasValue)
        {
            return null;
        }

        return await _dbContext.OrderExecutionRecords.AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.SourceCommandId == latestCommandId.Value,
                cancellationToken);
    }

    public Task<List<ProductionExecutionRecord>> ListProductionExecutionRecordsAsync(
        Guid sourceCommandId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductionExecutionRecords.AsNoTracking()
            .Where(record => record.SourceCommandId == sourceCommandId)
            .OrderBy(record => record.SourceProductionJobId)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Order> ApplyFiltersAndScope(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.Orders.WhereNotDeleted();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(searchLower) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(searchLower)) ||
                (o.CustomerPhoneNumber != null && o.CustomerPhoneNumber.ToLower().Contains(searchLower)));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (paymentStatus.HasValue)
        {
            query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(o => o.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(o => o.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(o => o.KioskId == kioskId.Value);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgs = allowedOrganizationIds ?? Array.Empty<Guid>();
            var allowedStores = allowedStoreIds ?? Array.Empty<Guid>();
            var allowedKiosks = allowedKioskIds ?? Array.Empty<Guid>();

            query = query.Where(o =>
                (o.OrganizationId.HasValue && allowedOrgs.Contains(o.OrganizationId.Value)) ||
                (o.StoreId.HasValue && allowedStores.Contains(o.StoreId.Value)) ||
                allowedKiosks.Contains(o.KioskId));
        }

        return query;
    }

    public Task<Order?> GetOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.WhereNotDeleted()
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Options)
            .FirstOrDefaultAsync(order => order.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AcquireIdempotencyLockAsync(
        string scopedIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({scopedIdempotencyKey}, 0));",
            cancellationToken);
    }

    public Task<Order?> GetOrderByClientOrderIdAsync(Guid kioskId, string clientOrderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.WhereNotDeleted()
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Options)
            .FirstOrDefaultAsync(
                order => order.KioskId == kioskId && order.ClientOrderId == clientOrderId,
                cancellationToken);
    }

    public async Task AddOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderStatusHistories.AddAsync(history, cancellationToken);
    }

    public async Task AddOrderItemStatusHistoryAsync(OrderItemStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderItemStatusHistories.AddAsync(history, cancellationToken);
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

    public async Task<T> ExecuteCheckoutTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);

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

}
