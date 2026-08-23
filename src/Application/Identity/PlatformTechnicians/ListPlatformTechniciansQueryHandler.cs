using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts;
using Application.Shared.Wrappers;

namespace Application.Identity.PlatformTechnicians;

public sealed class ListPlatformTechniciansQueryHandler(IIdentityAccountStore accounts)
{
    public async Task<PagedResult<TechnicianResult>> HandleAsync(
        string? search,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var technicians = await accounts.ListTechniciansAsync(search, page, size, cancellationToken);
        var total = await accounts.CountTechniciansAsync(search, cancellationToken);
        return PagedResult<TechnicianResult>.Success(
            technicians.Select(PlatformTechnicianResultMapper.ToResult),
            total,
            page,
            size);
    }

    public async Task<ApiResult<TechnicianResult>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetByIdAsync(id, true, cancellationToken);
        if (account?.PlatformTechnicianProfile is null ||
            PlatformTechnicianBoundary.HasMixedActiveRoles(account))
        {
            return ApiResult<TechnicianResult>.Fail("Technician account not found.", 404);
        }

        return ApiResult<TechnicianResult>.Success(PlatformTechnicianResultMapper.ToResult(account));
    }
}
