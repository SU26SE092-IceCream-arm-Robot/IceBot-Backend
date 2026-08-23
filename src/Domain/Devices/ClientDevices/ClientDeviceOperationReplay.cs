using Domain.Common;

namespace Domain.Devices.ClientDevices;

public sealed class ClientDeviceOperationReplay : BusinessEntity
{
    public Guid KioskId { get; set; }
    public Guid? ClientDeviceId { get; set; }
    public string Operation { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public string RequestFingerprint { get; set; } = null!;
    public Guid ResultClientDeviceId { get; set; }
}
