namespace Application.Shared.Ownership;

public enum TechnicalResourceKind
{
    Product = 0,
    ProductVariant = 1,
    Recipe = 2,
    ProductOption = 3,
    RobotArtifact = 4,
    RobotProgram = 5,
    ConfigurationRelease = 6
}

public interface ITechnicalResourceMutationPolicy
{
    Task<string?> ValidateDefinitionMutationAsync(
        TechnicalResourceKind resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}
