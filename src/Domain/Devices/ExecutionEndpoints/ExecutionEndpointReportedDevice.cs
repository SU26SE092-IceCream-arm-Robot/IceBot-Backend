using Domain.Common;

namespace Domain.Devices.ExecutionEndpoints;

public class ExecutionEndpointReportedDevice : AuditedEntity
{
    public Guid KioskExecutionEndpointId { get; private set; }
    public Guid KioskId { get; private set; }
    public string SourceDeviceKey { get; private set; } = null!;
    public Guid? DeviceId { get; private set; }
    public string RuntimeTargetCode { get; private set; } = null!;
    public string MachineModelCode { get; private set; } = null!;
    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private ExecutionEndpointReportedDevice()
    {
    }

    internal static ExecutionEndpointReportedDevice Create(
        Guid endpointId,
        Guid kioskId,
        string sourceDeviceKey,
        Guid? deviceId,
        string runtimeTargetCode,
        string machineModelCode)
    {
        if (endpointId == Guid.Empty || kioskId == Guid.Empty ||
            string.IsNullOrWhiteSpace(sourceDeviceKey) ||
            string.IsNullOrWhiteSpace(runtimeTargetCode) ||
            string.IsNullOrWhiteSpace(machineModelCode) ||
            sourceDeviceKey.Trim().Length > 100 ||
            runtimeTargetCode.Trim().Length > 100 ||
            machineModelCode.Trim().Length > 100)
        {
            throw new DomainRuleException("Reported device identity and runtime profile are required.");
        }

        return new ExecutionEndpointReportedDevice
        {
            KioskExecutionEndpointId = endpointId,
            KioskId = kioskId,
            SourceDeviceKey = sourceDeviceKey.Trim(),
            DeviceId = deviceId,
            RuntimeTargetCode = runtimeTargetCode.Trim(),
            MachineModelCode = machineModelCode.Trim()
        };
    }
}

public sealed record ReportedDeviceSnapshotItem(
    string SourceDeviceKey,
    Guid? DeviceId,
    string RuntimeTargetCode,
    string MachineModelCode);

public enum ReportedDeviceSnapshotApplyDisposition
{
    Applied = 1,
    Duplicate = 2,
    Stale = 3
}
