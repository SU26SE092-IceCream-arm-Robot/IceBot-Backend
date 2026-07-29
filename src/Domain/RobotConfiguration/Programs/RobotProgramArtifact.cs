using Domain.Common;

namespace Domain.RobotConfiguration.Programs;

public class RobotProgramArtifact : RobotConfigurationEntity
{
    public Guid RobotProgramId { get; private set; }

    public Guid RobotArtifactId { get; private set; }

    public int RunOrder { get; private set; }

    public int ParametersSchemaVersion { get; private set; } = 1;

    public string? ParametersJson { get; private set; }

    public string? RequiredOptionCode { get; private set; }

    public virtual RobotProgram RobotProgram { get; private set; } = null!;

    private RobotProgramArtifact()
    {
    }

    internal static RobotProgramArtifact Create(
        Guid robotArtifactId,
        int runOrder,
        string? parametersJson = null,
        int parametersSchemaVersion = 1,
        string? requiredOptionCode = null)
    {
        if (robotArtifactId == Guid.Empty)
        {
            throw new DomainRuleException("Robot artifact id is required.");
        }

        if (runOrder <= 0)
        {
            throw new DomainRuleException("Robot program artifact run order must be greater than zero.");
        }

        if (parametersSchemaVersion <= 0)
        {
            throw new DomainRuleException("Robot program artifact parameters schema version must be greater than zero.");
        }

        return new RobotProgramArtifact
        {
            RobotArtifactId = robotArtifactId,
            RunOrder = runOrder,
            ParametersJson = parametersJson,
            ParametersSchemaVersion = parametersSchemaVersion,
            RequiredOptionCode = string.IsNullOrWhiteSpace(requiredOptionCode)
                ? null
                : requiredOptionCode.Trim().ToUpperInvariant()
        };
    }
}
