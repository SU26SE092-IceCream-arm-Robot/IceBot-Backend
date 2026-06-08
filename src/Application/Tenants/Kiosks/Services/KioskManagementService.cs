using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Requests;
using Application.Tenants.Kiosks.Results;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Tenants.Kiosks.Services;

public sealed class KioskManagementService
{
    private readonly IKioskStore _kioskStore;

    public KioskManagementService(IKioskStore kioskStore)
    {
        _kioskStore = kioskStore;
    }

    public async Task<ApiResult<IReadOnlyList<KioskResult>>> ListKiosksAsync(
        CurrentUserContext userContext,
        Guid? organizationId,
        Guid? storeId,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        KioskStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<KioskStatus>(status.Trim(), ignoreCase: true, out var resultStatus) ||
                !Enum.IsDefined(resultStatus))
            {
                return ApiResult<IReadOnlyList<KioskResult>>.Fail("Invalid kiosk status.", 400);
            }

            parsedStatus = resultStatus;
        }

        IReadOnlyList<Kiosk> list;
        if (userContext.IsSystemAdmin)
        {
            list = await _kioskStore.ListAsync(organizationId, storeId, parsedStatus, search, cancellationToken);
        }
        else
        {
            // If scoped filters are specified, but user doesn't have access to them, ListAccessibleAsync will automatically handle it
            list = await _kioskStore.ListAccessibleAsync(
                userContext.AllowedOrganizationIds,
                userContext.AllowedStoreIds,
                userContext.AllowedKioskIds,
                organizationId,
                storeId,
                parsedStatus,
                search,
                cancellationToken);
        }

        var results = list.Select(ToResult).ToList();
        return ApiResult<IReadOnlyList<KioskResult>>.Success(results);
    }

    public async Task<ApiResult<KioskResult>> GetKioskAsync(
        CurrentUserContext userContext,
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await _kioskStore.GetByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskResult>.Fail("Kiosk not found.", 404);
        }

        if (!CanAccessKiosk(userContext, kiosk))
        {
            return ApiResult<KioskResult>.Fail("Access denied.", 403);
        }

        return ApiResult<KioskResult>.Success(ToResult(kiosk));
    }

    public async Task<ApiResult<KioskResult>> CreateKioskAsync(
        CurrentUserContext userContext,
        Guid storeId,
        CreateKioskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResult<KioskResult>.Fail("Kiosk name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ApiResult<KioskResult>.Fail("Kiosk code is required.");
        }

        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
        {
            return ApiResult<KioskResult>.Fail("Latitude must be between -90 and 90.");
        }

        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
        {
            return ApiResult<KioskResult>.Fail("Longitude must be between -180 and 180.");
        }

        var store = await _kioskStore.GetStoreByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<KioskResult>.Fail("Store not found.", 404);
        }

        if (!CanManageStoreKiosks(userContext, store))
        {
            return ApiResult<KioskResult>.Fail("Access denied.", 403);
        }

        if (store.Status != EntityStatus.Active)
        {
            return ApiResult<KioskResult>.Fail("Parent store is inactive.");
        }

        var isOrgActive = await _kioskStore.OrganizationExistsActiveAsync(store.OrganizationId, cancellationToken);
        if (!isOrgActive)
        {
            return ApiResult<KioskResult>.Fail("Parent organization is inactive or does not exist.");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var codeExists = await _kioskStore.KioskCodeExistsAsync(store.OrganizationId, code, cancellationToken: cancellationToken);
        if (codeExists)
        {
            return ApiResult<KioskResult>.Fail($"Kiosk with code '{code}' already exists in this organization.", 409);
        }

        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = store.OrganizationId,
            StoreId = storeId,
            Code = code,
            Name = request.Name.Trim(),
            KioskType = string.IsNullOrWhiteSpace(request.KioskType) ? "RoboticVending" : request.KioskType.Trim(),
            Status = KioskStatus.Provisioning,
            SerialNumber = request.SerialNumber?.Trim(),
            TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Bangkok" : request.TimeZone.Trim(),
            Address = request.Address?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SupportsOfflineMode = request.SupportsOfflineMode,
            SettingsSchemaVersion = request.SettingsSchemaVersion,
            SettingsJson = request.SettingsJson,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = userContext.AccountId
        };

        await _kioskStore.AddAsync(kiosk, cancellationToken);
        await _kioskStore.SaveChangesAsync(cancellationToken);

        return ApiResult<KioskResult>.Success(ToResult(kiosk), "Kiosk created successfully.", 201);
    }

    public async Task<ApiResult<KioskResult>> UpdateKioskAsync(
        CurrentUserContext userContext,
        Guid kioskId,
        UpdateKioskRequest request,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await _kioskStore.GetByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskResult>.Fail("Kiosk not found.", 404);
        }

        if (!CanAccessKiosk(userContext, kiosk))
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
        kiosk.SupportsOfflineMode = request.SupportsOfflineMode;
        kiosk.SettingsSchemaVersion = request.SettingsSchemaVersion;
        kiosk.SettingsJson = request.SettingsJson;
        kiosk.UpdatedAt = DateTimeOffset.UtcNow;
        kiosk.UpdatedByAccountId = userContext.AccountId;

        await _kioskStore.SaveChangesAsync(cancellationToken);

        return ApiResult<KioskResult>.Success(ToResult(kiosk), "Kiosk updated successfully.");
    }

    public async Task<ApiResult<KioskResult>> SetKioskStatusAsync(
        CurrentUserContext userContext,
        Guid kioskId,
        SetKioskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await _kioskStore.GetByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskResult>.Fail("Kiosk not found.", 404);
        }

        if (!CanAccessKiosk(userContext, kiosk))
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

        return ApiResult<KioskResult>.Success(ToResult(kiosk), "Kiosk status updated successfully.");
    }

    private static bool CanAccessKiosk(CurrentUserContext userContext, Kiosk kiosk)
    {
        return userContext.IsSystemAdmin
            || userContext.AllowedOrganizationIds.Contains(kiosk.OrganizationId)
            || userContext.AllowedStoreIds.Contains(kiosk.StoreId)
            || userContext.AllowedKioskIds.Contains(kiosk.Id);
    }

    private static bool CanManageStoreKiosks(CurrentUserContext userContext, Store store)
    {
        return userContext.IsSystemAdmin
            || userContext.AllowedOrganizationIds.Contains(store.OrganizationId)
            || userContext.AllowedStoreIds.Contains(store.Id);
    }

    private static KioskResult ToResult(Kiosk kiosk)
    {
        return new KioskResult
        {
            Id = kiosk.Id,
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            Code = kiosk.Code,
            Name = kiosk.Name,
            KioskType = kiosk.KioskType,
            Status = kiosk.Status.ToString(),
            SerialNumber = kiosk.SerialNumber,
            TimeZone = kiosk.TimeZone,
            Address = kiosk.Address,
            Latitude = kiosk.Latitude,
            Longitude = kiosk.Longitude,
            InstalledAt = kiosk.InstalledAt,
            LastOnlineAt = kiosk.LastOnlineAt,
            SupportsOfflineMode = kiosk.SupportsOfflineMode,
            ConfigurationVersion = kiosk.ConfigurationVersion,
            SettingsSchemaVersion = kiosk.SettingsSchemaVersion,
            SettingsJson = kiosk.SettingsJson,
            CreatedAt = kiosk.CreatedAt,
            UpdatedAt = kiosk.UpdatedAt
        };
    }
}
