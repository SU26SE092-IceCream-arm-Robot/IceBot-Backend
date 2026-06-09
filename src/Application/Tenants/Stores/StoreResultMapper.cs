using Application.Tenants.Stores.Results;
using Domain.Tenants.Entities;

namespace Application.Tenants.Stores;

internal static class StoreResultMapper
{
    public static StoreResult ToResult(Store store)
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
}
