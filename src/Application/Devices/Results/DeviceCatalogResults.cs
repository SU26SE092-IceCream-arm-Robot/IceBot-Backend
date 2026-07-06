namespace Application.Devices.Results;

public sealed record DeviceTypeResult(
    long Id,
    string Code,
    string Name,
    string? Description,
    string Category,
    bool RequiresKioskAssignment,
    bool IsActive,
    int DisplayOrder);

public sealed record DeviceModelResult(
    Guid Id,
    long DeviceTypeId,
    string Code,
    string Name,
    string? Manufacturer,
    string? ModelNumber,
    string? FirmwareFamily,
    IReadOnlyList<string> Capabilities);
