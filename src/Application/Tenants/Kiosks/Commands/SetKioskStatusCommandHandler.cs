using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Results;
using Domain.Tenants.Enums;

namespace Application.Tenants.Kiosks.Commands;

public sealed class SetKioskStatusCommandHandler
{
    private readonly IKioskStore _kioskStore;

    public SetKioskStatusCommandHandler(IKioskStore kioskStore)
    {
        _kioskStore = kioskStore;
    }

    public async Task<ApiResult<KioskResult>> HandleAsync(
        SetKioskStatusCommand command,
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

        if (!KioskAccessRules.CanAccessKiosk(userContext, kiosk))
        {
            return ApiResult<KioskResult>.Fail("Access denied.", 403);
        }

        if (!Enum.IsDefined(request.Status))
        {
            return ApiResult<KioskResult>.Fail("Invalid kiosk status.", 400);
        }

        if (request.Status == KioskStatus.Active)
        {
            var isStoreActive = await _kioskStore.StoreExistsActiveAsync(kiosk.StoreId, cancellationToken);
            if (!isStoreActive)
            {
                return ApiResult<KioskResult>.Fail("Parent store is inactive.");
            }

            var isOrgActive = await _kioskStore.OrganizationExistsActiveAsync(kiosk.OrganizationId, cancellationToken);
            if (!isOrgActive)
            {
                return ApiResult<KioskResult>.Fail("Parent organization is inactive.");
            }
        }

        kiosk.Status = request.Status;
        kiosk.UpdatedAt = DateTimeOffset.UtcNow;
        kiosk.UpdatedByAccountId = userContext.AccountId;

        await _kioskStore.SaveChangesAsync(cancellationToken);

        return ApiResult<KioskResult>.Success(KioskResultMapper.ToResult(kiosk), "Kiosk status updated successfully.");
    }
}
