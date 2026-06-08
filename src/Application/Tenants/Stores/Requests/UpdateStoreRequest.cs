using System.ComponentModel.DataAnnotations;

namespace Application.Tenants.Stores.Requests;

public sealed class UpdateStoreRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    public string StoreType { get; set; } = "Retail";

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? Country { get; set; }

    [Required]
    public string TimeZone { get; set; } = "Asia/Bangkok";

    [Range(-90.0, 90.0)]
    public decimal? Latitude { get; set; }

    [Range(-180.0, 180.0)]
    public decimal? Longitude { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public int OpeningHoursSchemaVersion { get; set; } = 1;

    public string? OpeningHoursJson { get; set; }
}
