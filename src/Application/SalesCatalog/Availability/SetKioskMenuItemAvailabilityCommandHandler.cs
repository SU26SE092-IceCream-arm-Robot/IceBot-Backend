using Application.SalesCatalog.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.SalesCatalog.Availability;

public sealed class SetKioskMenuItemAvailabilityCommandHandler(IMenuStore menus)
{
    public async Task<ApiResult<KioskMenuItemAvailabilityResult>> HandleAsync(
        SetKioskMenuItemAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestError = Validate(command.Request);
        if (requestError is not null)
        {
            return ApiResult<KioskMenuItemAvailabilityResult>.Fail(requestError, 400);
        }

        try
        {
            return await menus.ExecuteInTransactionAsync(async ct =>
            {
            var kiosk = await menus.GetKioskByIdAsync(command.KioskId, ct);
            if (kiosk is null || !ScopeAccessRules.CanAccessScopedRow(
                    ScopeRoleSets.MenuItemAvailabilityManage,
                    command.UserContext,
                    kiosk.OrganizationId,
                    kiosk.StoreId,
                    kiosk.Id))
            {
                return ApiResult<KioskMenuItemAvailabilityResult>.Fail("Kiosk not found.", 404);
            }

            var menuEntry = (await menus.ListMenusForKioskAvailabilityAsync(
                    kiosk.OrganizationId, kiosk.StoreId, kiosk.Id, DateTimeOffset.UtcNow, ct))
                .SelectMany(menu => menu.MenuItems.Select(item => new { Menu = menu, Item = item }))
                .FirstOrDefault(entry => entry.Item.Id == command.MenuItemId);
            if (menuEntry is null)
            {
                return ApiResult<KioskMenuItemAvailabilityResult>.Fail("Menu item not found.", 404);
            }

            var requestId = command.Request.RequestId!.Trim();
            var existingReplay = await menus.GetKioskMenuItemAvailabilityByRequestIdAsync(
                kiosk.Id, command.MenuItemId, requestId, ct);
            if (existingReplay is not null)
            {
                if (existingReplay.RequestedState != command.Request.State ||
                    existingReplay.RequestedReasonCode != command.Request.ReasonCode ||
                    !string.Equals(existingReplay.RequestedReason, command.Request.Reason!.Trim(), StringComparison.Ordinal))
                {
                    return ApiResult<KioskMenuItemAvailabilityResult>.Fail(
                        "Request id was already used with a different availability change.", 409);
                }

                return ApiResult<KioskMenuItemAvailabilityResult>.Success(
                    ToResult(menuEntry.Menu.Name, menuEntry.Item.DisplayName, new KioskMenuItemAvailabilitySnapshot(
                        existingReplay.KioskId,
                        existingReplay.MenuId,
                        existingReplay.MenuItemId,
                        existingReplay.RequestedState,
                        existingReplay.AppliedRevision,
                        existingReplay.RequestedReasonCode,
                        existingReplay.RequestedReason,
                        existingReplay.AppliedAt,
                        existingReplay.AppliedByAccountId)),
                    "Menu item availability request was already applied.");
            }

            var availability = await menus.GetTrackedKioskMenuItemAvailabilityAsync(
                kiosk.Id, command.MenuItemId, ct);
            var currentRevision = availability?.Revision ?? 0;
            if (currentRevision != command.Request.ExpectedRevision)
            {
                return ApiResult<KioskMenuItemAvailabilityResult>.Fail(
                    "Menu item availability changed by another operator. Reload and try again.", 409);
            }

            var currentState = availability?.State ?? MenuItemOperationalAvailabilityState.Available;
            if (currentState == command.Request.State)
            {
                var unchanged = availability is null
                    ? new KioskMenuItemAvailabilitySnapshot(kiosk.Id, menuEntry.Menu.Id, menuEntry.Item.Id,
                        MenuItemOperationalAvailabilityState.Available, 0,
                        command.Request.ReasonCode, command.Request.Reason!.Trim(), DateTimeOffset.UtcNow,
                        command.UserContext.AccountId)
                    : new KioskMenuItemAvailabilitySnapshot(availability.KioskId, availability.MenuId,
                        availability.MenuItemId, availability.State, availability.Revision, availability.ReasonCode,
                        availability.Reason, availability.ChangedAt, availability.ChangedByAccountId);
                return ApiResult<KioskMenuItemAvailabilityResult>.Success(
                    ToResult(menuEntry.Menu.Name, menuEntry.Item.DisplayName, unchanged),
                    "Menu item availability is already in the requested state.");
            }

            var isNewAvailability = availability is null;
            availability ??= new KioskMenuItemAvailability
            {
                OrganizationId = kiosk.OrganizationId,
                StoreId = kiosk.StoreId,
                KioskId = kiosk.Id,
                MenuId = menuEntry.Menu.Id,
                MenuItemId = menuEntry.Item.Id,
                State = MenuItemOperationalAvailabilityState.Available,
                Revision = 0,
                ReasonCode = command.Request.ReasonCode,
                Reason = command.Request.Reason!.Trim(),
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedByAccountId = command.UserContext.AccountId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByAccountId = command.UserContext.AccountId
            };
            if (isNewAvailability)
            {
                await menus.AddKioskMenuItemAvailabilityAsync(availability, ct);
            }

            var actorScope = ScopeAccessRules.GetAuthorizingScopeSnapshots(
                    ScopeRoleSets.MenuItemAvailabilityManage,
                    command.UserContext,
                    kiosk.OrganizationId,
                    kiosk.StoreId,
                    kiosk.Id)
                .FirstOrDefault();
            if (actorScope is null)
            {
                return ApiResult<KioskMenuItemAvailabilityResult>.Fail("Access denied.", 403);
            }

            availability.Change(
                command.Request.State,
                command.Request.ReasonCode,
                command.Request.Reason!.Trim(),
                command.UserContext.AccountId,
                actorScope.RoleCode,
                requestId,
                DateTimeOffset.UtcNow);
            await menus.SaveChangesAsync(ct);

            var snapshot = new KioskMenuItemAvailabilitySnapshot(
                availability.KioskId, availability.MenuId, availability.MenuItemId, availability.State,
                availability.Revision, availability.ReasonCode, availability.Reason, availability.ChangedAt,
                availability.ChangedByAccountId);
            return ApiResult<KioskMenuItemAvailabilityResult>.Success(
                ToResult(menuEntry.Menu.Name, menuEntry.Item.DisplayName, snapshot));
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResult<KioskMenuItemAvailabilityResult>.Fail(
                "Menu item availability changed by another operator. Reload and try again.", 409);
        }
        catch (DbUpdateException)
        {
            return ApiResult<KioskMenuItemAvailabilityResult>.Fail(
                "Menu item availability changed concurrently. Reload and try again.", 409);
        }
    }

    private static string? Validate(SetKioskMenuItemAvailabilityRequest request)
    {
        if (!Enum.IsDefined(request.State) || !Enum.IsDefined(request.ReasonCode))
        {
            return "Availability state and reason code are required.";
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
        {
            return "Availability reason is required and must be 1000 characters or fewer.";
        }

        if (string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Trim().Length > 200)
        {
            return "Request id is required and must be 200 characters or fewer.";
        }

        return request.ExpectedRevision < 0 ? "Expected revision cannot be negative." : null;
    }

    private static KioskMenuItemAvailabilityResult ToResult(
        string menuName,
        string displayName,
        KioskMenuItemAvailabilitySnapshot snapshot) =>
        new()
        {
            KioskId = snapshot.KioskId,
            MenuId = snapshot.MenuId,
            MenuItemId = snapshot.MenuItemId,
            MenuName = menuName,
            DisplayName = displayName,
            CatalogSellable = true,
            State = snapshot.State,
            ReasonCode = snapshot.State == MenuItemOperationalAvailabilityState.Paused ? snapshot.ReasonCode : null,
            Reason = snapshot.State == MenuItemOperationalAvailabilityState.Paused ? snapshot.Reason : null,
            Revision = snapshot.Revision,
            ChangedAt = snapshot.ChangedAt,
            ChangedByAccountId = snapshot.ChangedByAccountId
        };
}
