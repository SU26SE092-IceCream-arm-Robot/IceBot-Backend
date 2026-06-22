namespace Application.Devices.Results;

public sealed class DeviceResult
{
    public Guid Id { get; set; }
    public Guid? KioskId { get; set; }
    public string? KioskCode { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? OrganizationId { get; set; }
    public long DeviceTypeId { get; set; }
    public string DeviceTypeCode { get; set; } = null!;
    public Guid? DeviceModelId { get; set; }
    public string? DeviceModelCode { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? SerialNumber { get; set; }
    public string Status { get; set; } = null!;
    public string? PositionLabel { get; set; }
    public string? FirmwareVersion { get; set; }
    public DateTimeOffset? InstalledAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
