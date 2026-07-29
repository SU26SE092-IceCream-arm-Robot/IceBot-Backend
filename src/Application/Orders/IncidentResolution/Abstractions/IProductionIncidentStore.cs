using Domain.Orders.Entities;
using Domain.Orders.Incidents;
using Domain.ProductionExecution.Projections;

namespace Application.Orders.IncidentResolution.Abstractions;

public interface IProductionIncidentStore
{
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task AcquireIncidentLockAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task AcquireSourceLockAsync(Guid sourceCommandId, Guid sourceProductionJobId, CancellationToken cancellationToken = default);
    Task<ProductionIncident?> GetByIdAsync(Guid incidentId, bool tracked, CancellationToken cancellationToken = default);
    Task<ProductionIncident?> GetBySourceAsync(Guid sourceCommandId, Guid sourceProductionJobId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ProductionIncident> Items, int Total)> ListAsync(
        ProductionIncidentStatus? status, Guid? organizationId, Guid? storeId, Guid? kioskId,
        bool isSystemAdmin, IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds, IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ProductionExecutionRecord?> GetProductionRecordAsync(
        Guid sourceCommandId, Guid sourceProductionJobId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductionIncident incident, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

