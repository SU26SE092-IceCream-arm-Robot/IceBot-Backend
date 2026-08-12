namespace Application.Tenants.Abstractions;

public interface IOrganizationAccessStateReader
{
    Task<bool> IsActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<OrganizationScopeReference>> FilterActiveScopesAsync(
        IReadOnlyCollection<OrganizationScopeReference> scopes,
        CancellationToken cancellationToken = default);
}

public sealed record OrganizationScopeReference(
    string RoleCode,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid? KioskId);
