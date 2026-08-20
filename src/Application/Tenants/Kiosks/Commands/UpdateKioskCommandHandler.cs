using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Results;

namespace Application.Tenants.Kiosks.Commands;

public sealed class UpdateKioskCommandHandler
{
    private readonly IKioskStore _kioskStore;

    public UpdateKioskCommandHandler(IKioskStore kioskStore)
    {
        _kioskStore = kioskStore;
    }

    public async Task<ApiResult<KioskResult>> HandleAsync(
        UpdateKioskCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var kioskId = command.KioskId;
        var request = command.Request;

        var kiosk = await _kioskStore.GetByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskResult>.Fail("Kiosk not found.", 404);
        }

        if (!KioskAccessRules.CanAccessKiosk(ScopeRoleSets.KiosksUpdate, userContext, kiosk))
        {
            return ApiResult<KioskResult>.Fail("Access denied.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResult<KioskResult>.Fail("Kiosk name is required.");
        }

        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
        {
            return ApiResult<KioskResult>.Fail("Latitude must be between -90 and 90.");
        }

        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
        {
            return ApiResult<KioskResult>.Fail("Longitude must be between -180 and 180.");
        }

        kiosk.Name = request.Name.Trim();
        kiosk.KioskType = string.IsNullOrWhiteSpace(request.KioskType) ? "RoboticVending" : request.KioskType.Trim();
        kiosk.SerialNumber = request.SerialNumber?.Trim();
        kiosk.TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Bangkok" : request.TimeZone.Trim();
        kiosk.Address = request.Address?.Trim();
        kiosk.Latitude = request.Latitude;
        kiosk.Longitude = request.Longitude;
        kiosk.UpdatedAt = DateTimeOffset.UtcNow;
        kiosk.UpdatedByAccountId = userContext.AccountId;

        await _kioskStore.SaveChangesAsync(cancellationToken);

        return ApiResult<KioskResult>.Success(KioskResultMapper.ToResult(kiosk), "Kiosk updated successfully.");
    }
}
