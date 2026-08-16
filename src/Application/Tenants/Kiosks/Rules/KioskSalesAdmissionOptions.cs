namespace Application.Tenants.Kiosks.Rules;

public sealed class KioskSalesAdmissionOptions
{
    public const string SectionName = "KioskSalesAdmission";

    // Set false only for a controlled demo where the operator accepts that Edge is unavailable.
    public bool RequireConnectivity { get; set; } = true;
}
