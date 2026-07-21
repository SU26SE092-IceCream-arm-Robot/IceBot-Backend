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
        if (!Enum.IsDefined(command.Request.Status))
        {
            return ApiResult<KioskResult>.Fail("Invalid kiosk status.", 400);
        }

        KioskStatusChangedEvent? changedEvent = null;
        var result = await _kioskStore.ExecuteOperationalStateSerializedAsync(
            command.KioskId,
            async ct =>
            {
                var kiosk = await _kioskStore.GetByIdAsync(command.KioskId, ct);
                if (kiosk is null)
                {
                    return ApiResult<KioskResult>.Fail("Kiosk not found.", 404);
                }

                if (!KioskAccessRules.CanAccessKiosk(
                        ScopeRoleSets.KiosksManage,
                        command.UserContext,
                        kiosk))
                {
                    return ApiResult<KioskResult>.Fail("Access denied.", 403);
                }

                if (kiosk.Status == command.Request.Status)
                {
                    return ApiResult<KioskResult>.Success(
                        KioskResultMapper.ToResult(kiosk),
                        "Kiosk status is unchanged.");
                }

                if (command.Request.Status == KioskStatus.Active)
                {
                    if (!await _kioskStore.StoreExistsActiveAsync(kiosk.StoreId, ct))
                    {
                        return ApiResult<KioskResult>.Fail("Parent store is inactive.");
                    }
                    if (!await _kioskStore.OrganizationExistsActiveAsync(kiosk.OrganizationId, ct))
                    {
                        return ApiResult<KioskResult>.Fail("Parent organization is inactive.");
                    }
                }

                var oldStatus = kiosk.Status;
                var changedAt = DateTimeOffset.UtcNow;
                kiosk.Status = command.Request.Status;
                kiosk.UpdatedAt = changedAt;
                kiosk.UpdatedByAccountId = command.UserContext.AccountId;
                await _kioskStore.SaveChangesAsync(ct);

                changedEvent = new KioskStatusChangedEvent
                {
                    KioskId = kiosk.Id,
                    OrganizationId = kiosk.OrganizationId,
                    StoreId = kiosk.StoreId,
                    OldLifecycleStatus = oldStatus.ToString(),
                    NewLifecycleStatus = command.Request.Status.ToString(),
                    Reason = "KioskStatusUpdated",
                    UpdatedAt = changedAt,
                    Version = 1
                };
                return ApiResult<KioskResult>.Success(
                    KioskResultMapper.ToResult(kiosk),
                    "Kiosk status updated successfully.");
            },
            cancellationToken);

        if (changedEvent is not null)
        {
            await _publisher.PublishKioskStatusChangedAsync(changedEvent, cancellationToken);
        }

        return result;
    }
}
