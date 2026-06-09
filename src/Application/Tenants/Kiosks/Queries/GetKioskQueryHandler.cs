using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Results;

namespace Application.Tenants.Kiosks.Queries;

public sealed class GetKioskQueryHandler
{
    private readonly IKioskStore _kioskStore;

    public GetKioskQueryHandler(IKioskStore kioskStore)
    {
        _kioskStore = kioskStore;
    }

    public async Task<ApiResult<KioskResult>> HandleAsync(
        GetKioskQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await _kioskStore.GetByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskResult>.Fail("Kiosk not found.", 404);
        }

        if (!KioskAccessRules.CanAccessKiosk(query.UserContext, kiosk))
        {
            return ApiResult<KioskResult>.Fail("Access denied.", 403);
        }

        return ApiResult<KioskResult>.Success(KioskResultMapper.ToResult(kiosk));
    }
}
