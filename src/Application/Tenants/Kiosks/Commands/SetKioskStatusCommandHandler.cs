using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Results;
using Domain.Tenants.Enums;

namespace Application.Tenants.Kiosks.Commands;

public sealed class SetKioskStatusCommandHandler
{
    private readonly IKioskStore _kioskStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public SetKioskStatusCommandHandler(IKioskStore kioskStore, IRealtimeNotificationPublisher publisher)
    {
        _kioskStore = kioskStore;
        _publisher = publisher;
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

        if (!KioskAccessRules.CanAccessKiosk(ScopeRoleSets.KiosksManage, userContext, kiosk))
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

        var oldStatus = kiosk.Status;
        if (oldStatus == request.Status)
        {
            return ApiResult<KioskResult>.Success(KioskResultMapper.ToResult(kiosk), "Kiosk status is unchanged.");
        }

        var changedAt = DateTimeOffset.UtcNow;
        kiosk.Status = request.Status;
        kiosk.UpdatedAt = changedAt;
        kiosk.UpdatedByAccountId = userContext.AccountId;

        await _kioskStore.SaveChangesAsync(cancellationToken);

        var kioskStatusEvent = new KioskStatusChangedEvent
        {
            KioskId = kiosk.Id,
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            OldLifecycleStatus = oldStatus.ToString(),
            NewLifecycleStatus = request.Status.ToString(),
            Reason = "KioskStatusUpdated",
            UpdatedAt = changedAt,
            Version = 1
        };
        await _publisher.PublishKioskStatusChangedAsync(kioskStatusEvent, cancellationToken);

        return ApiResult<KioskResult>.Success(KioskResultMapper.ToResult(kiosk), "Kiosk status updated successfully.");
    }
}
