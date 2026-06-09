using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Kiosks.Results;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Application.Tenants.Kiosks.Commands;

public sealed class CreateKioskCommandHandler
{
    private readonly IKioskStore _kioskStore;

    public CreateKioskCommandHandler(IKioskStore kioskStore)
    {
        _kioskStore = kioskStore;
    }

    public async Task<ApiResult<KioskResult>> HandleAsync(
        CreateKioskCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var storeId = command.StoreId;
        var request = command.Request;

        var store = await _kioskStore.GetStoreByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<KioskResult>.Fail("Store not found.", 404);
        }

        if (!KioskAccessRules.CanManageStoreKiosks(userContext, store))
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

        return ApiResult<KioskResult>.Success(KioskResultMapper.ToResult(kiosk), "Kiosk created successfully.", 201);
    }
}
