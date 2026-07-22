using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Domain.Catalog.Enums;
using Domain.Devices.Catalog;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Devices.ExecutionEndpoints.Projections;
using Application.Orders.Support;
using Application.Tenants.Kiosks.Rules;
using Domain.Tenants.Enums;

namespace Infrastructure.EdgeIntegration.Persistence;

public sealed class OrderExecutionDispatchStore : IOrderExecutionDispatchStore
{
    private readonly IceBotDbContext _dbContext;

    public OrderExecutionDispatchStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteSerializedAsync<T>(
        Guid orderId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({OrderWorkflowConcurrency.OrderLockKey(orderId)}, 0));",
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task AcquireEndpointAdmissionLockAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"execution-endpoint-admission:{endpointId:D}"}, 0));",
            cancellationToken);
    }

    public Task AcquireKioskOperationalLockAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({KioskOperationalConcurrency.LockKey(kioskId)}, 0));",
            cancellationToken);

    public Task<bool> IsKioskOperationalAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Kiosks.WhereNotDeleted().AnyAsync(
            kiosk => kiosk.Id == kioskId &&
                kiosk.Status == Domain.Tenants.Enums.KioskStatus.Active &&
                kiosk.OperationalState == KioskOperationalState.Operational,
            cancellationToken);

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.WhereNotDeleted()
            .Include(order => order.Kiosk)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Options)
                    .ThenInclude(option => option.IngredientRequirements)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<KioskExecutionEndpoint>> ListActiveEndpointsAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.KioskExecutionEndpoints.WhereNotDeleted()
            .Include(endpoint => endpoint.CredentialBinding)
            .Where(endpoint =>
                endpoint.KioskId == kioskId &&
                endpoint.Status == KioskExecutionEndpointStatus.Active &&
                endpoint.CredentialBinding != null &&
                endpoint.CredentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Active)
            .OrderBy(endpoint => endpoint.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<ExecutionEndpointReadinessProjection?> GetReadinessAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default) =>
        _dbContext.ExecutionEndpointReadinessProjections.AsNoTracking()
            .Include(x => x.Capabilities)
            .FirstOrDefaultAsync(x => x.KioskExecutionEndpointId == endpointId, cancellationToken);

    public Task<ConfigurationRelease?> GetReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default)
    {
        return ReleaseGraph().FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<ControllerArtifactSetDeployment?> GetControllerActiveSetAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments
            .Include(deployment => deployment.Items)
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> ListReadyIngredientIdsAsync(
        Guid kioskId,
        IReadOnlyCollection<Guid> ingredientIds,
        CancellationToken cancellationToken = default)
    {
        if (ingredientIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = await _dbContext.IngredientDispenserStates
            .Where(state => state.KioskId == kioskId && state.IsActive && ingredientIds.Contains(state.IngredientId))
            .Where(state =>
                state.Ingredient.IsActive &&
                state.Device.Status == DeviceStatus.Online &&
                state.LevelToQuantityProfileJson != null)
            .Select(state => state.IngredientId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public Task<EdgeCommand?> GetCommandAsync(
        Guid orderId,
        int dispatchAttemptNo,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AsNoTracking().FirstOrDefaultAsync(command =>
            command.OrderId == orderId &&
            command.DispatchAttemptNo == dispatchAttemptNo,
            cancellationToken);
    }

    public Task<EdgeCommand?> GetLatestCommandAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands
            .Where(command =>
                command.CommandType == EdgeCommandType.ExecuteOrder &&
                command.OrderId == orderId)
            .OrderByDescending(command => command.DispatchAttemptNo)
            .ThenByDescending(command => command.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<EdgeCommand?> GetCommandByIdAsync(
        Guid commandId,
        CancellationToken cancellationToken = default) =>
        _dbContext.EdgeCommands.AsNoTracking()
            .FirstOrDefaultAsync(command => command.Id == commandId, cancellationToken);

    public Task<List<Domain.ProductionExecution.Projections.ProductionExecutionRecord>>
        ListProductionExecutionRecordsForOrderItemAsync(
            Guid orderId,
            Guid orderItemId,
            CancellationToken cancellationToken = default) =>
        _dbContext.ProductionExecutionRecords.AsNoTracking()
            .Include(record => record.SourceCommand)
            .Where(record =>
                record.SourceCommand.OrderId == orderId &&
                record.OrderItemId == orderItemId)
            .OrderBy(record => record.SourceCommand.DispatchAttemptNo)
            .ThenBy(record => record.ProductionUnitNo)
            .ToListAsync(cancellationToken);

    public Task AddOrderStatusHistoryAsync(
        OrderStatusHistory history,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories.AddAsync(history, cancellationToken).AsTask();
    }

    public Task<int> CountActiveCommandsAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.CountAsync(command =>
            command.TargetExecutionEndpointId == endpointId &&
            command.CommandType == EdgeCommandType.ExecuteOrder &&
            (command.Status == EdgeCommandStatus.PendingDelivery ||
                command.Status == EdgeCommandStatus.Delivered ||
                (command.Status == EdgeCommandStatus.Accepted &&
                    !_dbContext.OrderExecutionRecords.Any(record =>
                        record.SourceCommandId == command.Id &&
                        (record.Status == ProductionExecutionStatus.Completed ||
                            record.Status == ProductionExecutionStatus.Failed ||
                            record.Status == ProductionExecutionStatus.RequiresManualIntervention)))),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListReadyOrderIdsWithoutInitialCommandAsync(
        int maxOrders,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders.WhereNotDeleted().AsNoTracking()
            .Where(order =>
                order.PaymentStatus == PaymentStatus.Paid &&
                order.Status == OrderStatus.ReadyForFulfillment &&
                order.Kiosk.Status == Domain.Tenants.Enums.KioskStatus.Active &&
                order.Kiosk.OperationalState == KioskOperationalState.Operational &&
                order.OrderItems.Any(item => item.FulfillmentType == FulfillmentType.MachineProduced) &&
                !_dbContext.EdgeCommands.Any(command =>
                    command.OrderId == order.Id &&
                    command.DispatchAttemptNo == 1))
            .OrderBy(order => order.PaidAt)
            .ThenBy(order => order.Id)
            .Select(order => order.Id)
            .Take(maxOrders)
            .ToListAsync(cancellationToken);
    }

    public Task AddCommandAsync(EdgeCommand command, CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AddAsync(command, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ConfigurationRelease> ReleaseGraph()
    {
        return _dbContext.ConfigurationReleases.WhereNotDeleted()
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.ProductVariant)
                    .ThenInclude(variant => variant.Product)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.Recipe)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.RobotBindings)
                    .ThenInclude(binding => binding.RobotProgram);
    }
}
