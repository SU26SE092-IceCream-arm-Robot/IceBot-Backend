using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Results;
using Domain.Common.Enums;
using Domain.Tenants.Entities;

namespace Application.Tenants.Stores.Commands;

public sealed class CreateStoreCommandHandler
{
    private readonly IStoreStore _storeStore;

    public CreateStoreCommandHandler(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task<ApiResult<StoreResult>> HandleAsync(
        CreateStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var organizationId = command.OrganizationId;
        var request = command.Request;

        if (!StoreAccessRules.CanManageOrganizationStores(
                ScopeRoleSets.StoresManage, userContext, organizationId))
        {
            return ApiResult<StoreResult>.Fail("Access denied.", 403);
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

        var openingHoursError = StoreOpeningHoursContract.Validate(request.OpeningHours);
        if (openingHoursError is not null)
        {
            return ApiResult<StoreResult>.Fail(openingHoursError, 400);
        }

        var timeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Bangkok" : request.TimeZone.Trim();
        var timeZoneError = StoreSalesAvailabilityRules.ValidateTimeZone(timeZone);
        if (timeZoneError is not null)
        {
            return ApiResult<StoreResult>.Fail(timeZoneError, 400);
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
            TimeZone = timeZone,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            OpeningHoursSchemaVersion = 1,
            OpeningHoursJson = StoreOpeningHoursContract.Serialize(request.OpeningHours),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = userContext.AccountId
        };

        await _storeStore.AddAsync(store, cancellationToken);
        await _storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<StoreResult>.Success(StoreResultMapper.ToResult(store), "Store created successfully.");
    }
}
