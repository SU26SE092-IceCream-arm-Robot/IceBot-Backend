using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Results;
using Domain.Common;
using Domain.Tenants.Enums;

namespace Application.Tenants.Kiosks.Commands;

public sealed class SetKioskOperationalStateCommandHandler
{
    private readonly IKioskStore _store;
    private readonly IRealtimeNotificationPublisher _publisher;

    public SetKioskOperationalStateCommandHandler(
        IKioskStore store,
        IRealtimeNotificationPublisher publisher)
    {
        _store = store;
        _publisher = publisher;
    }

    public async Task<ApiResult<KioskResult>> HandleAsync(
        SetKioskOperationalStateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.StoreId == Guid.Empty || command.KioskId == Guid.Empty)
        {
            return ApiResult<KioskResult>.Fail("Store and kiosk are required.", 400);
        }

        if (!Enum.IsDefined(command.Request.State) || string.IsNullOrWhiteSpace(command.Request.Reason))
        {
            return ApiResult<KioskResult>.Fail("A valid operational state and reason are required.", 400);
        }

        KioskOperationalStateChangedEvent? changedEvent = null;
        var result = await _store.ExecuteOperationalStateSerializedAsync(
            command.KioskId,
            async ct =>
            {
                var kiosk = await _store.GetByStoreAndIdAsync(command.StoreId, command.KioskId, ct);
                if (kiosk is null)
                {
                    return ApiResult<KioskResult>.Fail("Kiosk not found in the specified store.", 404);
                }

                if (!KioskAccessRules.CanAccessKiosk(
                        ScopeRoleSets.KiosksOperationsManage,
                        command.UserContext,
                        kiosk))
                {
                    return ApiResult<KioskResult>.Fail("Access denied.", 403);
                }

                if (kiosk.OperationalState == command.Request.State)
                {
                    return ApiResult<KioskResult>.Success(
                        KioskResultMapper.ToResult(kiosk),
                        "Kiosk operational state is unchanged.");
                }

                if (RequiresIdleExecution(command.Request.State) &&
                    await _store.HasRunningExecutionAsync(kiosk.Id, ct))
                {
                    return ApiResult<KioskResult>.Fail(
                        "Kiosk cannot enter this operational state while an execution is running. Use EmergencyStopRequested to hold new work and request immediate safety intervention.",
                        409);
                }

                var oldState = kiosk.OperationalState;
                var changedAt = DateTimeOffset.UtcNow;
                try
                {
                    var transition = kiosk.ChangeOperationalState(
                        command.Request.State,
                        command.Request.Reason,
                        command.UserContext.AccountId,
                        changedAt);
                    if (transition is not null)
                    {
                        await _store.AddOperationalStateTransitionAsync(transition, ct);
                    }
                }
                catch (DomainRuleException exception)
                {
                    return ApiResult<KioskResult>.Fail(exception.Message, 400);
                }

                await _store.SaveChangesAsync(ct);
                changedEvent = new KioskOperationalStateChangedEvent
                {
                    KioskId = kiosk.Id,
                    OrganizationId = kiosk.OrganizationId,
                    StoreId = kiosk.StoreId,
                    OldState = oldState.ToString(),
                    NewState = kiosk.OperationalState.ToString(),
                    Reason = kiosk.OperationalStateReason!,
                    ChangedByAccountId = command.UserContext.AccountId,
                    ChangedAt = changedAt
                };
                return ApiResult<KioskResult>.Success(
                    KioskResultMapper.ToResult(kiosk),
                    "Kiosk operational state updated successfully.");
            },
            cancellationToken);

        if (changedEvent is not null)
        {
            await _publisher.PublishKioskOperationalStateChangedAsync(changedEvent, cancellationToken);
        }

        return result;
    }

    private static bool RequiresIdleExecution(KioskOperationalState state) =>
        state is KioskOperationalState.Maintenance or
            KioskOperationalState.Cleaning or
            KioskOperationalState.Restocking;
}
