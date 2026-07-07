namespace Application.Devices.Results;

public sealed class DeviceReplacementResult
{
    public Guid SourceDeviceId { get; set; }
    public Guid ReplacementDeviceId { get; set; }
    public int ReboundContainerCount { get; set; }
    public IReadOnlyList<Guid> ReplacementDispenserStateIds { get; set; } = [];
}
