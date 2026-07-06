using System.ComponentModel.DataAnnotations;

namespace Application.Devices.Requests;

public sealed class CreateDeviceTypeRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(50)]
    public string Category { get; set; } = "Peripheral";

    public bool RequiresKioskAssignment { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}

public sealed class UpdateDeviceTypeRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(50)]
    public string Category { get; set; } = "Peripheral";

    public bool RequiresKioskAssignment { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}

public sealed class SetDeviceTypeStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class CreateDeviceModelRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Manufacturer { get; set; }

    [StringLength(100)]
    public string? ModelNumber { get; set; }

    [StringLength(100)]
    public string? FirmwareFamily { get; set; }

    public IReadOnlyList<string> Capabilities { get; set; } = [];
}

public sealed class UpdateDeviceModelRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Manufacturer { get; set; }

    [StringLength(100)]
    public string? ModelNumber { get; set; }

    [StringLength(100)]
    public string? FirmwareFamily { get; set; }

    public IReadOnlyList<string> Capabilities { get; set; } = [];
}
