using Application.Tenants.Organizations.ReadModels;

namespace Application.Tenants.Organizations.Abstractions;

public interface IOrganizationSalesSummaryStore
{
    Task<int> CountAsync(OrganizationSalesSummaryReadRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationSalesSummaryReadModel>> ListAsync(
        OrganizationSalesSummaryReadRequest request,
        CancellationToken cancellationToken = default);
}
