namespace Application.Tenants.Kiosks.Results;

public sealed class KioskResult
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string KioskType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? SerialNumber { get; set; }
    public string TimeZone { get; set; } = null!;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset? InstalledAt { get; set; }
    public DateTimeOffset? LastOnlineAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
