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
            OperationalState = kiosk.OperationalState.ToString(),
            OperationalStateReason = kiosk.OperationalStateReason,
            OperationalStateChangedAt = kiosk.OperationalStateChangedAt,
            OperationalStateChangedByAccountId = kiosk.OperationalStateChangedByAccountId,
            SerialNumber = kiosk.SerialNumber,
            TimeZone = kiosk.TimeZone,
            Address = kiosk.Address,
            Latitude = kiosk.Latitude,
            Longitude = kiosk.Longitude,
            InstalledAt = kiosk.InstalledAt,
            LastOnlineAt = kiosk.LastOnlineAt,
            CreatedAt = kiosk.CreatedAt,
            UpdatedAt = kiosk.UpdatedAt
        };
    }
}
