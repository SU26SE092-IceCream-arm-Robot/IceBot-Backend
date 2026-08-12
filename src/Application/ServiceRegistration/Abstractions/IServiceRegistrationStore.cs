using ServiceRegistrationEntity = Domain.ServiceRegistration.Entities.ServiceRegistration;

namespace Application.ServiceRegistration.Abstractions;

public interface IServiceRegistrationStore
{
    Task<ServiceRegistrationEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<ServiceRegistrationEntity?> GetAsync(Guid id, bool tracked, CancellationToken cancellationToken = default);
    Task<bool> PrivacyPolicyRevisionIsPublishedAsync(Guid revisionId, CancellationToken cancellationToken = default);
    Task<bool> TryAddAsync(ServiceRegistrationEntity registration, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search, Domain.ServiceRegistration.Enums.ServiceRegistrationStatus? status, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceRegistrationEntity>> ListAsync(string? search, Domain.ServiceRegistration.Enums.ServiceRegistrationStatus? status, DateTimeOffset? from, DateTimeOffset? to, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

public sealed record ServiceRegistrationProvisioningOutcome(
    bool Succeeded,
    int StatusCode,
    string Message,
    ServiceRegistrationEntity? Registration,
    string? InvitationUrl,
    bool InvitationEmailSent);

public interface IServiceRegistrationProvisioner
{
    Task<ServiceRegistrationProvisioningOutcome> ProvisionAsync(
        Guid registrationId,
        Guid actorId,
        ServiceRegistrationProvisioningRequest request,
        bool retry,
        CancellationToken cancellationToken = default);
}
