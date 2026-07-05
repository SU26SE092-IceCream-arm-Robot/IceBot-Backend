using System.ComponentModel.DataAnnotations;

namespace Application.Devices.Requests;

public sealed class CreateDeviceRequest
{
    [Required]
    public long DeviceTypeId { get; set; }

    public Guid? DeviceModelId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Code { get; set; } = null!;

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
