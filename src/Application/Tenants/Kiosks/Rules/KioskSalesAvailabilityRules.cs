using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Application.Tenants.Kiosks.Rules;

internal static class KioskSalesAvailabilityRules
{
    public static string? ValidateOnlineSalesAvailability(Kiosk kiosk)
    {
        if (kiosk.Status != KioskStatus.Active)
        {
            return "Kiosk is not active for sales.";
        }

        if (kiosk.Store is null || kiosk.Organization is null)
        {
            return "Kiosk sales scope could not be verified.";
        }

        if (kiosk.Store.Status != EntityStatus.Active)
        {
            return "Store is not active for sales.";
        }

        if (kiosk.Organization.Status != EntityStatus.Active)
        {
            return "Organization is not active for sales.";
        }

        return null;
    }
}
