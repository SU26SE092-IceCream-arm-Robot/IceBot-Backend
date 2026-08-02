using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks.Rules;
using Application.Tenants.Stores;
using Application.SalesCatalog.RuntimeMenus.Services;

namespace Application.SalesCatalog.RuntimeMenus.Queries;

public sealed class GetKioskRuntimeMenuQueryHandler
{
    private readonly IMenuStore _menus;
    private readonly RuntimeMenuProjectionBuilder _projectionBuilder;
    private readonly IRuntimeMenuProjectionCache _cache;

    public GetKioskRuntimeMenuQueryHandler(
        IMenuStore menus,
        RuntimeMenuProjectionBuilder projectionBuilder,
        IRuntimeMenuProjectionCache cache)
    {
        _menus = menus;
        _projectionBuilder = projectionBuilder;
        _cache = cache;
    }

    public async Task<ApiResult<RuntimeMenuResult>> HandleAsync(
        GetKioskRuntimeMenuQuery query,
        CancellationToken cancellationToken = default)
    {
        var kioskId = query.KioskId;
        var kiosk = await _menus.GetKioskByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<RuntimeMenuResult>.Fail("Kiosk not found.", 404);
        }

        var connectivity = await _menus.GetKioskConnectivityAsync(kioskId, cancellationToken);
        var salesAvailabilityError = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(kiosk, connectivity);
        if (salesAvailabilityError is not null)
        {
            return ApiResult<RuntimeMenuResult>.Fail(salesAvailabilityError, 409);
        }

        var now = DateTimeOffset.UtcNow;
        var admissionError = StoreSalesAvailabilityRules.ValidateSalesAdmission(kiosk.Store, now);
        if (admissionError is not null)
        {
            return ApiResult<RuntimeMenuResult>.Fail(admissionError, 409);
        }

        var projection = await _cache.GetOrCreateAsync(
            kiosk.Id,
            ct => _projectionBuilder.BuildAsync(kiosk, ct),
            cancellationToken);

        var result = new RuntimeMenuResult
        {
            SnapshotId = Guid.CreateVersion7(),
            Revision = projection.Revision,
            KioskId = kiosk.Id,
            GeneratedAt = now,
            ExpiresAt = projection.ValidUntil,
            Items = projection.Items
        };

        return ApiResult<RuntimeMenuResult>.Success(result);
    }
}
