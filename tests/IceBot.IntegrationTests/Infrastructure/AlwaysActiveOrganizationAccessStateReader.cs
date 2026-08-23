using Application.Tenants.Abstractions;

namespace IceBot.IntegrationTests.Infrastructure;

internal sealed class AlwaysActiveOrganizationAccessStateReader : IOrganizationAccessStateReader
{
    public Task<bool> IsActiveAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<IReadOnlySet<OrganizationScopeReference>> FilterActiveScopesAsync(
        IReadOnlyCollection<OrganizationScopeReference> scopes,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<OrganizationScopeReference>>(
            scopes.ToHashSet());
}
