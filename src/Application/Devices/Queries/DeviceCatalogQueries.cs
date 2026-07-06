namespace Application.Devices.Queries;

public sealed record ListDeviceTypesQuery(string? Search, bool? IsActive);
public sealed record GetDeviceTypeQuery(long DeviceTypeId);
public sealed record ListDeviceModelsQuery(long DeviceTypeId, string? Search);
public sealed record GetDeviceModelQuery(Guid DeviceModelId);
