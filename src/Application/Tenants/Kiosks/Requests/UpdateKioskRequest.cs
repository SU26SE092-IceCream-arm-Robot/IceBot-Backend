using System.ComponentModel.DataAnnotations;

namespace Application.Tenants.Kiosks.Requests;

public sealed class UpdateKioskRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    public string KioskType { get; set; } = "RoboticVending";

    public string? SerialNumber { get; set; }

    [Required]
    public string TimeZone { get; set; } = "Asia/Bangkok";

    public string? Address { get; set; }

    [Range(-90.0, 90.0)]
    public decimal? Latitude { get; set; }

    [Range(-180.0, 180.0)]
    public decimal? Longitude { get; set; }

    public bool SupportsOfflineMode { get; set; } = true;

}
