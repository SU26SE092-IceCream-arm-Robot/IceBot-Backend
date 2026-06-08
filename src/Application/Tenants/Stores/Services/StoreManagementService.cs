using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Requests;
using Application.Tenants.Stores.Results;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Tenants.Stores.Services;

public sealed class StoreManagementService
{
    private readonly IStoreStore _storeStore;

    public StoreManagementService(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task<ApiResult<IReadOnlyList<StoreResult>>> ListStoresAsync(
        CurrentUserContext userContext,
        Guid? organizationId,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        EntityStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<EntityStatus>(status.Trim(), ignoreCase: true, out var resultStatus))
            {
                return ApiResult<IReadOnlyList<StoreResult>>.Fail("Invalid store status.", 400);
            }

            parsedStatus = resultStatus;
        }

        if (userContext.IsSystemAdmin)
        {
            var list = await _storeStore.ListAsync(organizationId, parsedStatus, search, cancellationToken);
            return ApiResult<IReadOnlyList<StoreResult>>.Success(list.Select(ToResult).ToList());
        }

        var accessibleStores = await _storeStore.ListAccessibleAsync(
            userContext.AllowedOrganizationIds,
            userContext.AllowedStoreIds,
            organizationId,
            parsedStatus,
            search,
            cancellationToken);

        return ApiResult<IReadOnlyList<StoreResult>>.Success(accessibleStores.Select(ToResult).ToList());
    }

    public async Task<ApiResult<StoreResult>> GetStoreAsync(
        CurrentUserContext userContext,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var store = await _storeStore.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<StoreResult>.Fail("Store not found.", 404);
        }

        if (!CanAccessStore(userContext, store))
        {
            return ApiResult<StoreResult>.Fail("Access denied.", 403);
        }

        return ApiResult<StoreResult>.Success(ToResult(store));
    }

    public async Task<ApiResult<StoreResult>> CreateStoreAsync(
        CurrentUserContext userContext,
        Guid organizationId,
        CreateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CanManageOrganizationStores(userContext, organizationId))
        {
            return ApiResult<StoreResult>.Fail("Access denied.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResult<StoreResult>.Fail("Store name is required.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ApiResult<StoreResult>.Fail("Store code is required.", 400);
        }

        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
        {
            return ApiResult<StoreResult>.Fail("Latitude must be between -90 and 90.", 400);
        }

        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
        {
            return ApiResult<StoreResult>.Fail("Longitude must be between -180 and 180.", 400);
        }

        // Parent organization must be active
        var isOrgActive = await _storeStore.OrganizationExistsActiveAsync(organizationId, cancellationToken);
        if (!isOrgActive)
        {
            return ApiResult<StoreResult>.Fail("Parent organization is inactive or does not exist.", 400);
        }

        // Code uniqueness validation
        var code = request.Code.Trim().ToUpperInvariant();
        var codeExists = await _storeStore.StoreCodeExistsAsync(organizationId, code, cancellationToken: cancellationToken);
        if (codeExists)
        {
            return ApiResult<StoreResult>.Fail($"Store with code '{code}' already exists in this organization.", 409);
        }

        var store = new Store
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = code,
            Name = request.Name.Trim(),
            StoreType = string.IsNullOrWhiteSpace(request.StoreType) ? "Retail" : request.StoreType.Trim(),
            Status = EntityStatus.Active,
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            Province = request.Province?.Trim(),
            Country = request.Country?.Trim(),
            TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Bangkok" : request.TimeZone.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            OpeningHoursSchemaVersion = request.OpeningHoursSchemaVersion,
            OpeningHoursJson = request.OpeningHoursJson,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = userContext.AccountId
        };

        await _storeStore.AddAsync(store, cancellationToken);
        await _storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<StoreResult>.Success(ToResult(store), "Store created successfully.");
    }

    public async Task<ApiResult<StoreResult>> UpdateStoreAsync(
        CurrentUserContext userContext,
        Guid storeId,
        UpdateStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var store = await _storeStore.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<StoreResult>.Fail("Store not found.", 404);
        }

        if (!CanAccessStore(userContext, store))
        {
            return ApiResult<StoreResult>.Fail("Access denied.", 403);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResult<StoreResult>.Fail("Store name is required.", 400);
        }

        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
        {
            return ApiResult<StoreResult>.Fail("Latitude must be between -90 and 90.", 400);
        }

        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
        {
            return ApiResult<StoreResult>.Fail("Longitude must be between -180 and 180.", 400);
        }

        store.Name = request.Name.Trim();
        store.StoreType = string.IsNullOrWhiteSpace(request.StoreType) ? "Retail" : request.StoreType.Trim();
        store.Address = request.Address?.Trim();
        store.City = request.City?.Trim();
        store.Province = request.Province?.Trim();
        store.Country = request.Country?.Trim();
        store.TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Bangkok" : request.TimeZone.Trim();
        store.Latitude = request.Latitude;
        store.Longitude = request.Longitude;
        store.PhoneNumber = request.PhoneNumber?.Trim();
        store.Email = request.Email?.Trim().ToLowerInvariant();
        store.OpeningHoursSchemaVersion = request.OpeningHoursSchemaVersion;
        store.OpeningHoursJson = request.OpeningHoursJson;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        store.UpdatedByAccountId = userContext.AccountId;

        await _storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<StoreResult>.Success(ToResult(store), "Store updated successfully.");
    }

    public async Task<ApiResult<bool>> DisableStoreAsync(
        CurrentUserContext userContext,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var store = await _storeStore.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<bool>.Fail("Store not found.", 404);
        }

        if (!CanManageOrganizationStores(userContext, store.OrganizationId))
        {
            return ApiResult<bool>.Fail("Access denied.", 403);
        }

        store.Status = EntityStatus.Inactive;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        store.UpdatedByAccountId = userContext.AccountId;

        await _storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Store disabled successfully.");
    }

    public async Task<ApiResult<bool>> ActivateStoreAsync(
        CurrentUserContext userContext,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var store = await _storeStore.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<bool>.Fail("Store not found.", 404);
        }

        if (!CanManageOrganizationStores(userContext, store.OrganizationId))
        {
            return ApiResult<bool>.Fail("Access denied.", 403);
        }

        var isOrgActive = await _storeStore.OrganizationExistsActiveAsync(store.OrganizationId, cancellationToken);
        if (!isOrgActive)
        {
            return ApiResult<bool>.Fail("Parent organization is inactive or does not exist.", 400);
        }

        store.Status = EntityStatus.Active;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        store.UpdatedByAccountId = userContext.AccountId;

        await _storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Store activated successfully.");
    }

    private static StoreResult ToResult(Store store)
    {
        return new StoreResult
        {
            Id = store.Id,
            OrganizationId = store.OrganizationId,
            Code = store.Code,
            Name = store.Name,
            StoreType = store.StoreType,
            Status = store.Status.ToString(),
            Address = store.Address,
            City = store.City,
            Province = store.Province,
            Country = store.Country,
            TimeZone = store.TimeZone,
            Latitude = store.Latitude,
            Longitude = store.Longitude,
            PhoneNumber = store.PhoneNumber,
            Email = store.Email,
            OpeningHoursSchemaVersion = store.OpeningHoursSchemaVersion,
            OpeningHoursJson = store.OpeningHoursJson,
            CreatedAt = store.CreatedAt,
            UpdatedAt = store.UpdatedAt
        };
    }

    private static bool CanAccessStore(CurrentUserContext userContext, Store store)
    {
        return userContext.IsSystemAdmin ||
               userContext.AllowedOrganizationIds.Contains(store.OrganizationId) ||
               userContext.AllowedStoreIds.Contains(store.Id);
    }

    private static bool CanManageOrganizationStores(CurrentUserContext userContext, Guid organizationId)
    {
        return userContext.IsSystemAdmin ||
               userContext.AllowedOrganizationIds.Contains(organizationId);
    }
}
