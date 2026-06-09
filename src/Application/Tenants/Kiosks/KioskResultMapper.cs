using Application.Tenants.Kiosks.Results;
using Domain.Tenants.Entities;

namespace Application.Tenants.Kiosks;

internal static class KioskResultMapper
{
    public static KioskResult ToResult(Kiosk kiosk)
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
