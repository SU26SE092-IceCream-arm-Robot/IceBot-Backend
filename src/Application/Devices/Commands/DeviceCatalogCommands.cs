using Application.Devices.Requests;

namespace Application.Devices.Commands;

public sealed record CreateDeviceTypeCommand(CreateDeviceTypeRequest Request, Guid? ActorId);
public sealed record UpdateDeviceTypeCommand(long DeviceTypeId, UpdateDeviceTypeRequest Request, Guid? ActorId);
public sealed record SetDeviceTypeStatusCommand(long DeviceTypeId, bool IsActive, Guid? ActorId);
public sealed record CreateDeviceModelCommand(long DeviceTypeId, CreateDeviceModelRequest Request, Guid? ActorId);
public sealed record UpdateDeviceModelCommand(Guid DeviceModelId, UpdateDeviceModelRequest Request, Guid? ActorId);
public sealed record RetireDeviceModelCommand(Guid DeviceModelId, Guid? ActorId);
