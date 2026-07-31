namespace Application.Shared.Concurrency;

public sealed record TechnicalResourceMutationIdentity(string ResourceType, string ResourceKey)
{
    public string AdvisoryLockKey => $"technical-resource:{ResourceType}:{ResourceKey}";
    public int LockTier => ResourceType.EndsWith("Definition", StringComparison.Ordinal) ? 0 : 1;

    public static TechnicalResourceMutationIdentity Contract(Guid id) => Resource("RobotArtifactTechnicalContract", id);
    public static TechnicalResourceMutationIdentity Artifact(Guid id) => Resource("RobotArtifact", id);
    public static TechnicalResourceMutationIdentity Template(Guid id) => Resource("RobotArtifactTemplate", id);
    public static TechnicalResourceMutationIdentity Program(Guid id) => Resource("RobotProgram", id);
    public static TechnicalResourceMutationIdentity ConfigurationRelease(Guid id) => Resource("ConfigurationRelease", id);
    public static TechnicalResourceMutationIdentity Product(Guid id) => Resource("Product", id);
    public static TechnicalResourceMutationIdentity Menu(Guid id) => Resource("Menu", id);
    public static TechnicalResourceMutationIdentity ExecutionEndpoint(Guid id) =>
        Resource("KioskExecutionEndpoint", id);
    public static TechnicalResourceMutationIdentity PackageInstallation(Guid id) =>
        Resource("ProductionPackageInstallation", id);

    public static TechnicalResourceMutationIdentity ContractDefinition(Guid? organizationId, string code, int version) =>
        new("RobotArtifactTechnicalContractDefinition",
            $"{organizationId?.ToString("D") ?? "global"}:{Normalize(code)}:{version}");

    public static TechnicalResourceMutationIdentity ArtifactDefinition(Guid organizationId, string code) =>
        new("RobotArtifactDefinition", $"{organizationId:D}:{Normalize(code)}");

    public static TechnicalResourceMutationIdentity ProgramDefinition(
        Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId, string code) =>
        new("RobotProgramDefinition",
            $"{organizationId:D}:{Value(storeId)}:{Value(kioskId)}:{Value(deviceId)}:{Normalize(code)}");

    private static TechnicalResourceMutationIdentity Resource(string type, Guid id) =>
        new(type, id.ToString("D"));

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string Value(Guid? value) => value?.ToString("D") ?? "-";

    public static IReadOnlyList<TechnicalResourceMutationIdentity> OrderForLocking(
        IEnumerable<TechnicalResourceMutationIdentity> resources) =>
        resources
            .Distinct()
            .OrderBy(item => item.LockTier)
            .ThenBy(item => item.ResourceType, StringComparer.Ordinal)
            .ThenBy(item => item.ResourceKey, StringComparer.Ordinal)
            .ToArray();
}

public interface ITechnicalResourceMutationCoordinator
{
    Task<T> ExecuteAsync<T>(
        IReadOnlyCollection<TechnicalResourceMutationIdentity> resources,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}

public sealed class InlineTechnicalResourceMutationCoordinator : ITechnicalResourceMutationCoordinator
{
    public static InlineTechnicalResourceMutationCoordinator Instance { get; } = new();

    private InlineTechnicalResourceMutationCoordinator() { }

    public Task<T> ExecuteAsync<T>(IReadOnlyCollection<TechnicalResourceMutationIdentity> resources,
        Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) =>
        action(cancellationToken);
}
