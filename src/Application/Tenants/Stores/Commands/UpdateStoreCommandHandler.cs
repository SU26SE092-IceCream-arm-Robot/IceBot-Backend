using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Results;

namespace Application.Tenants.Stores.Commands;

public sealed class UpdateStoreCommandHandler
{
    private readonly IStoreStore _storeStore;

    public UpdateStoreCommandHandler(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task<ApiResult<StoreResult>> HandleAsync(
        UpdateStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var storeId = command.StoreId;
        var request = command.Request;

        var store = await _storeStore.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<StoreResult>.Fail("Store not found.", 404);
        }

        if (!StoreAccessRules.CanAccessStore(userContext, store))
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

        return ApiResult<StoreResult>.Success(StoreResultMapper.ToResult(store), "Store updated successfully.");
    }
}
