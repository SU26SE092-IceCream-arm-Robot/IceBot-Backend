namespace Domain.ProductionConfiguration.ValueObjects;

public sealed record ControllerArtifactSetItemSelection(
    Guid ExecutionRouteId,
    Guid RobotProgramId,
    Guid RobotArtifactId,
    int RunOrder);
