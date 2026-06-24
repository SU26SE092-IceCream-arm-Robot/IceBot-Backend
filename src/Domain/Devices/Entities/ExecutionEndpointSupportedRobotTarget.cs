using Domain.Common;

namespace Domain.Devices.Entities;

public class ExecutionEndpointSupportedRobotTarget : AuditedEntity
{
    public Guid KioskExecutionEndpointId { get; private set; }

    public string RuntimeTargetCode { get; private set; } = null!;

    public string MachineModelCode { get; private set; } = null!;

    public Guid? DeviceId { get; private set; }

    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    public virtual Device? Device { get; private set; }

    private ExecutionEndpointSupportedRobotTarget()
    {
    }

    internal static ExecutionEndpointSupportedRobotTarget Create(
        Guid kioskExecutionEndpointId,
        string runtimeTargetCode,
        string machineModelCode,
        Device? device = null)
    {
        if (kioskExecutionEndpointId == Guid.Empty ||
            string.IsNullOrWhiteSpace(runtimeTargetCode) ||
            string.IsNullOrWhiteSpace(machineModelCode))
        {
            throw new DomainRuleException("Execution endpoint, runtime target code, and machine model code are required.");
        }

        return new ExecutionEndpointSupportedRobotTarget
        {
            KioskExecutionEndpointId = kioskExecutionEndpointId,
            RuntimeTargetCode = runtimeTargetCode.Trim(),
            MachineModelCode = machineModelCode.Trim(),
            DeviceId = device?.Id,
            Device = device
        };
    }
}
