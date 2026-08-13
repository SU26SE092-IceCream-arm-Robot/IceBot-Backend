using Application.Identity.Tokens.Claims;
using Domain.Catalog.Entities;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;

namespace Application.ProductionPackages.Installation;

public interface IProductionPackageInstallationStore
{
    Task<bool> ScopeExistsAsync(Guid organizationId, Guid? storeId, Guid? kioskId, CancellationToken cancellationToken);
    Task<ProductionPackageInstallation?> FindByIdempotencyKeyAsync(Guid organizationId, string key, CancellationToken cancellationToken);
    Task<ProductionPackageInstallation?> GetAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken);
    Task<ProductionPackageInstallation?> GetForEditAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken);
    Task<bool> TryRestartFailedAsync(Guid organizationId, Guid installationId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProductionPackageInstallationStatus?> GetCurrentStatusAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid organizationId, ProductionPackageInstallationStatus? status, Guid? storeId, Guid? kioskId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionPackageInstallation>> ListAsync(Guid organizationId, ProductionPackageInstallationStatus? status, Guid? storeId, Guid? kioskId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ProductionPackageInstallationInsertResult> InsertOrGetAsync(ProductionPackageInstallation installation, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid organizationId, Guid installationId, string failureCode, string failureMessage, CancellationToken cancellationToken);
    Task<ProductionPackageMaterializationRepairResult> RestoreSoftDeletedMaterializationsAsync(Guid organizationId, Guid installationId, Guid actorId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifact>> ListArtifactsByCodesAsync(Guid organizationId, IReadOnlyCollection<string> artifactCodes, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> ListPackageManagedArtifactIdsAsync(IReadOnlyCollection<Guid> artifactIds, CancellationToken cancellationToken);
    Task<ProductionPackageForkGraph?> GetForkGraphAsync(Guid organizationId, Guid installationId, bool tracked, CancellationToken cancellationToken);
    Task<bool> HasActiveUpgradeAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken);
    Task PersistForkAsync(ProductionPackageInstallation installation, IReadOnlyCollection<RobotArtifact> artifacts, IReadOnlyCollection<RobotProgramArtifact> removedProgramArtifacts, CancellationToken cancellationToken);
    Task<ConfigurationRelease> PersistMaterializedGraphAsync(ProductionPackageInstallation installation, IReadOnlyCollection<Product> products, IReadOnlyCollection<RobotArtifact> artifacts, IReadOnlyCollection<RobotProgram> programs, IReadOnlyCollection<ProductionComposition> compositions, Func<long, ConfigurationRelease> releaseFactory, CancellationToken cancellationToken);
}

public sealed record ProductionPackageInstallationInsertResult(bool Created, ProductionPackageInstallation Installation);

public sealed record ProductionPackageForkGraph(
    ProductionPackageInstallation Installation,
    IReadOnlyCollection<RobotArtifact> Artifacts,
    IReadOnlyCollection<RobotProgram> Programs,
    IReadOnlySet<Guid> SharedPackageManagedArtifactIds);

public sealed record ProductionPackageMaterializationExpectation(
    ProductionPackageResourceKind ResourceKind,
    string? SourceKey,
    Guid? ExpectedTargetId = null,
    int ExpectedCount = 1);

public sealed record ProductionPackageMaterializationRepairResult(
    IReadOnlyCollection<ProductionPackageMaterializationRepairItem> Restored,
    IReadOnlyCollection<ProductionPackageMaterializationRepairIssue> Issues);

public sealed record ProductionPackageMaterializationRepairItem(string ResourceKind, string SourceKey, string TargetKey);
public sealed record ProductionPackageMaterializationRepairIssue(string ResourceKind, string SourceKey, string TargetKey, string Code);
public sealed record ProductionPackageRepairResult(Guid InstallationId, IReadOnlyCollection<ProductionPackageMaterializationRepairItem> RestoredResources);

public sealed class InstallProductionPackageCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid PackageId { get; init; }
    public Guid PackageVersionId { get; init; }
    public required string IdempotencyKey { get; init; }
    public IReadOnlyCollection<string> ProductSourceKeys { get; init; } = [];
    internal string? MaterializationIdentitySuffix { get; init; }
}

public sealed record ProductionPackageInstallationResult(
    Guid Id,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    Guid PackageVersionId,
    string Status,
    string OwnershipMode,
    Guid? DraftConfigurationReleaseId,
    string? FailureCode,
    string? FailureMessage,
    IReadOnlyCollection<ProductionPackageMaterializationResult> Materializations)
{
    public static ProductionPackageInstallationResult From(ProductionPackageInstallation installation) => new(
        installation.Id, installation.OrganizationId, installation.StoreId, installation.KioskId,
        installation.PackageVersionId, installation.Status.ToString(), installation.OwnershipMode.ToString(),
        installation.DraftConfigurationReleaseId, installation.FailureCode, installation.FailureMessage,
        installation.Materializations.Select(x => new ProductionPackageMaterializationResult(
            x.ResourceKind.ToString(), x.SourceKey, x.TargetKey, x.TargetChecksum)).ToArray());
}

public sealed record ProductionPackageMaterializationResult(string ResourceKind, string SourceKey, string TargetKey, string? TargetChecksum);

public sealed record ProductionPackageInstallationPreview(
    Guid PackageVersionId,
    string ManifestChecksum,
    IReadOnlyCollection<string> ProductSourceKeys,
    IReadOnlyCollection<string> ProgramBlueprintCodes,
    IReadOnlyCollection<string> RouteCodes,
    IReadOnlyCollection<string> Warnings);

public static class ProductionPackageMaterializationExpectationBuilder
{
    public static IReadOnlyCollection<ProductionPackageMaterializationExpectation> Build(ProductionPackageInstallation installation)
    {
        var selectedProductKeys = installation.GetSelectedProductSourceKeys().ToHashSet(StringComparer.Ordinal);
        var version = installation.PackageVersion;
        var selectedProducts = version.Products.Where(x => selectedProductKeys.Contains(x.SourceKey)).ToArray();
        if (selectedProducts.Length != selectedProductKeys.Count)
            throw new DomainRuleException("Installation product selection no longer matches its immutable package version.");

        var expected = new List<ProductionPackageMaterializationExpectation>();
        foreach (var definition in selectedProducts)
        {
            var product = ProductionPackageProductSnapshotCodec.Deserialize(definition.ProductSnapshotJson).Product;
            expected.Add(new(ProductionPackageResourceKind.Product, definition.SourceKey));
            foreach (var variant in product.Variants)
            {
                expected.Add(new(ProductionPackageResourceKind.ProductVariant, SourceKey($"{definition.SourceKey}:VARIANT:{variant.Code}")));
                expected.AddRange(variant.Recipes.Select(recipe => new ProductionPackageMaterializationExpectation(
                    ProductionPackageResourceKind.Recipe,
                    SourceKey($"{definition.SourceKey}:VARIANT:{variant.Code}:RECIPE:{recipe.Code}"))));
            }

            expected.AddRange(product.OptionGroups.SelectMany(group => group.Options)
                .Select(option => new ProductionPackageMaterializationExpectation(
                    ProductionPackageResourceKind.ProductOption,
                    SourceKey($"{definition.SourceKey}:OPTION:{option.Code}"))));
        }

        var selection = ProductionPackageInstallationSelectionRules.Resolve(version, selectedProductKeys);
        expected.AddRange(selection.Artifacts.Select(artifact => new ProductionPackageMaterializationExpectation(
            ProductionPackageResourceKind.RobotArtifact, artifact.SourceKey)));
        expected.AddRange(selection.Routes.Select(route => new ProductionPackageMaterializationExpectation(
            ProductionPackageResourceKind.RobotProgram, route.RouteCode)));
        if (!installation.DraftConfigurationReleaseId.HasValue)
            throw new DomainRuleException("Installed package installation has no Draft configuration release identity.");
        expected.Add(new(ProductionPackageResourceKind.ConfigurationRelease, null, installation.DraftConfigurationReleaseId));
        return expected;
    }

    private static string SourceKey(string value) => value.Trim().ToUpperInvariant();
}
