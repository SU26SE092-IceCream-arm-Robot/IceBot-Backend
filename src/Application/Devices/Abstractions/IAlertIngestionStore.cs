using Domain.Operations.Entities;

namespace Application.Devices.Abstractions;

public interface IAlertIngestionStore
{
    Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default);
}
