using Domain.Operations.Entities;

namespace Application.Devices.Abstractions;

public interface IAlertIngestionStore
{
    Task<Alert?> FindCorrelatableAlertAsync(
        Guid kioskId,
        Guid deviceId,
        string correlationKey,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken = default);

    Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default);
}
