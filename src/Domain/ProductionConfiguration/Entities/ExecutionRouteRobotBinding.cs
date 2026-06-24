using Domain.Common;
using Domain.RobotConfiguration.Entities;

namespace Domain.ProductionConfiguration.Entities;

public class ExecutionRouteRobotBinding : BusinessEntity
{
    public Guid ExecutionRouteId { get; private set; }

    public int BindingOrder { get; private set; }

    public string RequiredWorkcellCapabilityCode { get; private set; } = null!;

    public Guid RobotProgramId { get; private set; }

    public virtual ExecutionRoute ExecutionRoute { get; private set; } = null!;

    public virtual RobotProgram RobotProgram { get; private set; } = null!;

    private ExecutionRouteRobotBinding()
    {
    }

    public static ExecutionRouteRobotBinding Create(
        Guid robotProgramId,
        int bindingOrder,
        string requiredWorkcellCapabilityCode)
    {
        if (robotProgramId == Guid.Empty)
        {
            throw new DomainRuleException("Robot program id is required.");
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
            RobotProgramId = robotProgramId,
            BindingOrder = bindingOrder,
            RequiredWorkcellCapabilityCode = requiredWorkcellCapabilityCode.Trim()
        };
    }
}
