namespace Application.Tenants.Kiosks.Rules;

public static class KioskOperationalConcurrency
{
    public static string LockKey(Guid kioskId) => $"kiosk-operational-state:{kioskId:D}";
}
