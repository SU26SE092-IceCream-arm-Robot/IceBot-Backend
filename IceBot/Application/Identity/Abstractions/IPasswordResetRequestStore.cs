using Domain.Identity.Entities;

namespace Application.Identity.Abstractions;

public interface IPasswordResetRequestStore
{
    Task<PasswordResetRequest?> GetByTokenHashAsync(
        string tokenHash,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task AddAsync(PasswordResetRequest passwordResetRequest, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
