using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Domain.Common;
using Domain.RobotConfiguration.Artifacts;

namespace Domain.ProductionConfiguration.Entities;

public class ExecutionRouteRobotBinding : BusinessEntity
{
    public Guid ExecutionRouteId { get; private set; }

    public int BindingOrder { get; private set; }

    public string RequiredWorkcellCapabilityCode { get; private set; } = null!;

    public Guid RobotProgramId { get; private set; }

    public Guid? ProductionProgramBindingId { get; private set; }

    public string? ProductionProgramBindingChecksum { get; private set; }

    public virtual ExecutionRoute ExecutionRoute { get; private set; } = null!;

    public virtual RobotProgram RobotProgram { get; private set; } = null!;

    private ExecutionRouteRobotBinding()
    {
    }

    internal static ExecutionRouteRobotBinding Create(
        Guid? productionProgramBindingId,
        string? productionProgramBindingChecksum,
        Guid robotProgramId,
        int bindingOrder,
        string requiredWorkcellCapabilityCode)
    {
        if ((productionProgramBindingId.HasValue && productionProgramBindingId == Guid.Empty) || robotProgramId == Guid.Empty)
        {
            throw new DomainRuleException("Production binding and robot program ids are required.");
        }

        if (bindingOrder <= 0)
        {
            throw new DomainRuleException("Execution route robot binding order must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(requiredWorkcellCapabilityCode))
        {
            throw new DomainRuleException("Required workcell capability code is required.");
        }

        return new ExecutionRouteRobotBinding
        {
            ProductionProgramBindingId = productionProgramBindingId,
            ProductionProgramBindingChecksum = productionProgramBindingId.HasValue
                ? RequireChecksum(productionProgramBindingChecksum!) : null,
            RobotProgramId = robotProgramId,
            BindingOrder = bindingOrder,
            RequiredWorkcellCapabilityCode = requiredWorkcellCapabilityCode.Trim()
        };
    }

    private static string RequireChecksum(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new DomainRuleException("Production binding checksum must be a SHA-256 checksum.");
        return normalized;
    }
}
