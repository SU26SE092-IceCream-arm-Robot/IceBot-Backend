using Domain.Common.Enums;
using Domain.Operations.Entities;
using Domain.Tenants.Entities;

namespace Application.Operations.OperationLogs.Abstractions;

public interface IOperationLogStore
{
    Task AddAsync(OperationLog operationLog, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid kioskId,
        Guid? deviceId,
        Guid? orderId,
        SeverityLevel? severity,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    Task<List<OperationLog>> ListAsync(
        Guid kioskId,
        Guid? deviceId,
        Guid? orderId,
        SeverityLevel? severity,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationLog?> GetByKioskIdAsync(
        Guid kioskId,
        Guid operationLogId,
        CancellationToken cancellationToken = default);
}
