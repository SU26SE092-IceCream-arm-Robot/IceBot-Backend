using Application.Operations.Alerts.Automation;
using Domain.Devices.ExecutionEndpoints;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class MqttCredentialAlertAutomationStore(IceBotDbContext db)
    : IMqttCredentialAlertAutomationStore
{
    private const string SourceType = "ExecutionEndpointMqttCredential";

    public async Task<IReadOnlyList<Guid>> ListFailureStateEndpointIdsAsync(
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await db.ExecutionEndpointMqttCredentials.AsNoTracking()
            .Where(credential =>
                credential.Status == ExecutionEndpointMqttCredentialStatus.RevokeFailed ||
                (credential.Status == ExecutionEndpointMqttCredentialStatus.Failed &&
                 credential.LastError != null &&
                 EF.Functions.ILike(credential.LastError, "%operation lease expired%")))
            .OrderBy(credential => credential.UpdatedAt ?? credential.CreatedAt)
            .ThenBy(credential => credential.Id)
            .Select(credential => credential.KioskExecutionEndpointId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListActiveAlertEndpointIdsAsync(
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await db.Alerts.AsNoTracking()
            .Where(alert =>
                alert.DeletedAt == null &&
                alert.SourceType == SourceType &&
                alert.SourceId.HasValue &&
                alert.Status != AlertStatus.Resolved &&
                alert.Status != AlertStatus.Suppressed)
            .GroupBy(alert => alert.SourceId!.Value)
            .Select(group => new
            {
                EndpointId = group.Key,
                LastOccurredAt = group.Max(alert => alert.LastOccurredAt)
            })
            .OrderBy(candidate => candidate.LastOccurredAt)
            .ThenBy(candidate => candidate.EndpointId)
            .Select(candidate => candidate.EndpointId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task<KioskExecutionEndpoint?> GetEndpointAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default) =>
        db.KioskExecutionEndpoints.WhereNotDeleted()
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.MqttCredential)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);

    public Task<List<Alert>> ListActiveAlertsAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default) =>
        db.Alerts.Where(alert =>
                alert.DeletedAt == null &&
                alert.SourceType == SourceType &&
                alert.SourceId == endpointId &&
                alert.Status != AlertStatus.Resolved &&
                alert.Status != AlertStatus.Suppressed)
            .ToListAsync(cancellationToken);

    public Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default) =>
        db.Alerts.AddAsync(alert, cancellationToken).AsTask();

    public Task AcquireLockAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        var lockKey = $"mqtt-credential-alert:{endpointId:D}";
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            db.ChangeTracker.Clear();
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
