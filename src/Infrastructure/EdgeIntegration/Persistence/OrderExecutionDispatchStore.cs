using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Domain.Catalog.Enums;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Devices.ExecutionEndpoints.Projections;

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
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"order-execution-dispatch:{orderId:D}"}, 0));",
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

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.ProductVariant)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<KioskExecutionEndpoint>> ListActiveEndpointsAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.KioskExecutionEndpoints
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
        return await _dbContext.Orders.AsNoTracking()
            .Where(order =>
                order.PaymentStatus == PaymentStatus.Paid &&
                order.Status == OrderStatus.ReadyForExecution &&
                order.OrderItems.Any(item => item.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced) &&
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
        return _dbContext.ConfigurationReleases
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
