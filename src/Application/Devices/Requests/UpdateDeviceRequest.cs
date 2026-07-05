using System.ComponentModel.DataAnnotations;

namespace Application.Devices.Requests;

public sealed class UpdateDeviceRequest
{
    [Required]
    public long DeviceTypeId { get; set; }

    public Guid? DeviceModelId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? PositionLabel { get; set; }

    [StringLength(100)]
    public string? FirmwareVersion { get; set; }

    public DateTimeOffset? InstalledAt { get; set; }

}
