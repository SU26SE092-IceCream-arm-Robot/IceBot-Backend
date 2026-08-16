using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Domain.Devices.Connectivity;

namespace Application.Tenants.Kiosks.Rules;

public static class KioskSalesAvailabilityRules
{
    public static string? ValidateOnlineSalesAvailability(
        Kiosk kiosk,
        KioskConnectivityProjection? connectivity,
        bool requireConnectivity = true)
    {
        if (kiosk.Status != KioskStatus.Active)
        {
            return "Kiosk is not active for sales.";
        }

        if (kiosk.OperationalState != KioskOperationalState.Operational)
        {
            return $"Kiosk is not accepting orders while its operational state is {kiosk.OperationalState}.";
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

        if (requireConnectivity &&
            connectivity?.Status is not (KioskConnectivityStatus.Online or KioskConnectivityStatus.Degraded))
        {
            return "Kiosk is not currently reachable for online sales.";
        }

        return null;
    }
}
