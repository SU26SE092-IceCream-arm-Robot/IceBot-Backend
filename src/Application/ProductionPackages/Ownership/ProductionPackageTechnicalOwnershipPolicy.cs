using Application.Shared.Ownership;
using Domain.ProductionPackages;

namespace Application.ProductionPackages.Ownership;

public interface IProductionPackageTechnicalOwnershipStore
{
    Task<bool> IsPackageManagedAsync(
        ProductionPackageResourceKind resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductionPackageResourceOwner>> ListOwnersAsync(
        ProductionPackageResourceKind resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}

public sealed record ProductionPackageResourceOwner(
    Guid InstallationId,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    string OwnershipMode,
    string Status);

public sealed class ProductionPackageTechnicalOwnershipPolicy(
    IProductionPackageTechnicalOwnershipStore store) : ITechnicalResourceMutationPolicy
{
    public async Task<string?> ValidateDefinitionMutationAsync(
        TechnicalResourceKind resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return await store.IsPackageManagedAsync(Map(resourceKind), resourceId, cancellationToken)
            ? "Package-managed technical configuration must be forked before its definition can be changed."
            : null;
    }

    private static ProductionPackageResourceKind Map(TechnicalResourceKind resourceKind) => resourceKind switch
    {
        TechnicalResourceKind.Product => ProductionPackageResourceKind.Product,
        TechnicalResourceKind.ProductVariant => ProductionPackageResourceKind.ProductVariant,
        TechnicalResourceKind.Recipe => ProductionPackageResourceKind.Recipe,
        TechnicalResourceKind.ProductOption => ProductionPackageResourceKind.ProductOption,
        TechnicalResourceKind.RobotArtifact => ProductionPackageResourceKind.RobotArtifact,
        TechnicalResourceKind.RobotProgram => ProductionPackageResourceKind.RobotProgram,
        TechnicalResourceKind.ConfigurationRelease => ProductionPackageResourceKind.ConfigurationRelease,
        _ => throw new ArgumentOutOfRangeException(nameof(resourceKind), resourceKind, null)
    };
}
