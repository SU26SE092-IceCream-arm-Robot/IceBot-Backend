using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Domain.Catalog.Entities;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.Artifacts;
using Domain.SalesCatalog.Entities;

namespace Application.ProductionPackages.Upgrades;

public sealed record ProductionPackageUpgradeResourceGraph(
    IReadOnlyDictionary<string, Product> Products,
    IReadOnlyDictionary<string, ProductVariant> Variants,
    IReadOnlyDictionary<string, Recipe> Recipes,
    IReadOnlyDictionary<string, ProductOption> Options,
    IReadOnlyDictionary<string, RobotArtifact> Artifacts);

public sealed record ProductionPackageUpgradeEndpointSnapshot(
    KioskExecutionEndpoint Endpoint,
    Guid ActiveReleaseId,
    Guid ActiveDeploymentId);

public sealed record ProductionPackageUpgradeSourceState(
    ProductionPackageInstallation SourceInstallation,
    ProductionPackageUpgradeResourceGraph SourceResources,
    IReadOnlyCollection<MenuItem> MenuItems,
    IReadOnlyCollection<ProductionPackageUpgradeEndpointSnapshot> EndpointTargets);

public sealed record ProductionPackageUpgradePreparationState(
    ProductionPackageUpgrade Upgrade,
    ProductionPackageInstallation SourceInstallation,
    ProductionPackageInstallation TargetInstallation,
    ProductionPackageUpgradeResourceGraph SourceResources,
    ProductionPackageUpgradeResourceGraph TargetResources,
    IReadOnlyCollection<MenuItem> MenuItems,
    IReadOnlyCollection<ProductionPackageUpgradeEndpointSnapshot> EndpointTargets);

public sealed record ProductionPackageUpgradeDeploymentObservation(
    Guid DeploymentId,
    ConfigurationDeploymentProfile Profile,
    Guid OrganizationId,
    Guid KioskId,
    Guid KioskExecutionEndpointId,
    Guid ConfigurationReleaseId,
    ConfigurationDeploymentReadStatus Status);

public sealed record ProductionPackageUpgradeMutationState(
    ProductionPackageUpgrade Upgrade,
    ProductionPackageInstallation SourceInstallation,
    ProductionPackageInstallation TargetInstallation,
    Domain.ProductionConfiguration.Entities.ConfigurationRelease TargetRelease,
    ProductionPackageUpgradeResourceGraph SourceResources,
    ProductionPackageUpgradeResourceGraph TargetResources,
    IReadOnlyCollection<MenuItem> MenuItems,
    IReadOnlyCollection<KioskExecutionEndpoint> Endpoints,
    IReadOnlyCollection<ProductionPackageUpgradeDeploymentObservation> ActiveDeployments);

public sealed record ProductionPackageUpgradeInsertResult(bool Created, ProductionPackageUpgrade Upgrade);

public sealed record ProductionPackageUpgradeRollbackAttemptRecordResult(
    ProductionPackageUpgrade Upgrade,
    bool Recorded,
    int AttemptNo);

public interface IProductionPackageUpgradeStore
{
    Task<ProductionPackageInstallation?> GetSourceInstallationAsync(
        Guid organizationId, Guid sourceInstallationId, CancellationToken cancellationToken);
    Task<ProductionPackageUpgradeSourceState?> GetSourceStateAsync(
        Guid organizationId, Guid sourceInstallationId, bool tracked, CancellationToken cancellationToken);
    Task<ProductionPackageUpgradePreparationState?> GetPreparationStateAsync(
        Guid organizationId, Guid upgradeId, Guid targetInstallationId, CancellationToken cancellationToken);
    Task<ProductionPackageUpgradeMutationState?> GetMutationStateAsync(
        Guid organizationId, Guid upgradeId, CancellationToken cancellationToken);
    Task<ProductionPackageUpgrade?> GetAsync(
        Guid organizationId, Guid sourceInstallationId, Guid upgradeId, bool tracked, CancellationToken cancellationToken);
    Task<ProductionPackageUpgrade?> FindByIdempotencyKeyAsync(
        Guid organizationId, string idempotencyKey, CancellationToken cancellationToken);
    Task<ProductionPackageUpgrade?> FindActiveBySourceInstallationAsync(
        Guid organizationId, Guid sourceInstallationId, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid organizationId, Guid sourceInstallationId,
        ProductionPackageUpgradeStatus? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionPackageUpgrade>> ListAsync(Guid organizationId, Guid sourceInstallationId,
        ProductionPackageUpgradeStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> ListConflictingProductCodesAsync(
        Guid organizationId, Guid? storeId, Guid? kioskId, IReadOnlyCollection<string> codes,
        IReadOnlyCollection<Guid> excludedProductIds, CancellationToken cancellationToken);
    Task<ProductionPackageUpgradeInsertResult> InsertOrGetAsync(
        ProductionPackageUpgrade upgrade, CancellationToken cancellationToken);
    Task<ProductionPackageUpgrade?> AttachTargetInstallationAsync(
        Guid organizationId, Guid upgradeId, Guid targetInstallationId, CancellationToken cancellationToken);
    Task<ProductionPackageUpgrade?> ResumeFailedAsync(
        Guid organizationId, Guid upgradeId, CancellationToken cancellationToken);
    Task SoftDeleteAbandonedTargetRootsAsync(
        Guid organizationId, Guid targetInstallationId, Guid actorId, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<bool> HasAbandonOperationalReferencesAsync(
        Guid organizationId, Guid targetInstallationId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> ListStaleMaterializingIdsAsync(
        DateTimeOffset cutoff, int batchSize, CancellationToken cancellationToken);
    Task<bool> TryFailStaleMaterializingAsync(
        Guid upgradeId, DateTimeOffset cutoff, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProductionPackageUpgradeRollbackAttemptRecordResult> RecordRollbackAttemptAsync(
        Guid organizationId, Guid sourceInstallationId, Guid upgradeId, Guid endpointId,
        Guid deploymentId, Guid actorId, string reason, DateTimeOffset now, int maxAttempts,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void ReplaceMenuItemProductOptions(MenuItem menuItem, IReadOnlyCollection<Guid> productOptionIds);
    Task MarkFailedAsync(Guid organizationId, Guid upgradeId, string code, string message,
        CancellationToken cancellationToken);
}

public sealed record ProductionPackageUpgradePreviewResult(
    Guid SourceInstallationId,
    Guid SourcePackageVersionId,
    Guid TargetPackageVersionId,
    string PreviewChecksum,
    IReadOnlyCollection<string> SelectedProductSourceKeys,
    IReadOnlyCollection<string> AddedProductSourceKeys,
    IReadOnlyCollection<string> RemovedProductSourceKeys,
    IReadOnlyCollection<string> ChangedProductSourceKeys,
    int AffectedMenuItemCount,
    int RequiredEndpointCount,
    IReadOnlyCollection<ProductionPackageUpgradeProductPreview> Products,
    IReadOnlyCollection<ProductionPackageUpgradeMenuPreview> MenuChanges,
    IReadOnlyCollection<ProductionPackageUpgradeArtifactPreview> Artifacts,
    IReadOnlyCollection<ProductionPackageUpgradeEndpointPreview> Endpoints,
    IReadOnlyCollection<string> Blockers,
    IReadOnlyCollection<string> Warnings);

public sealed record ProductionPackageUpgradePreviewContext(
    Application.Shared.Wrappers.ApiResult<ProductionPackageUpgradePreviewResult> Result,
    ProductionPackageUpgradeSourceState? SourceState = null,
    ProductionPackageVersion? TargetVersion = null)
{
    public static ProductionPackageUpgradePreviewContext Fail(string message, int statusCode) =>
        new(Application.Shared.Wrappers.ApiResult<ProductionPackageUpgradePreviewResult>.Fail(message, statusCode));
}

public sealed record ProductionPackageUpgradeProductPreview(
    string ProductSourceKey,
    string ChangeKind,
    string? CurrentProductCode,
    string? IncomingProductCode,
    int? CurrentPreparationTimeSeconds,
    int? IncomingPreparationTimeSeconds,
    bool PreservesCommercialFields,
    bool CurrentAvailability);

public sealed record ProductionPackageUpgradeMenuPreview(
    Guid MenuId,
    Guid MenuItemId,
    string MenuItemCode,
    string Action,
    string CurrentStatus,
    int? PreparationTimeOverrideSeconds);

public sealed record ProductionPackageUpgradeArtifactPreview(
    string ArtifactSourceKey,
    string ArtifactChecksum,
    string MaterializationAction);

public sealed record ProductionPackageUpgradeEndpointPreview(
    Guid KioskExecutionEndpointId,
    Guid KioskId,
    Guid SourceConfigurationReleaseId,
    Guid SourceDeploymentId);

public sealed record ProductionPackageUpgradeRollbackAttemptResult(
    int AttemptNo,
    Guid DeploymentId,
    Guid? ReplacedDeploymentId,
    string DeploymentStatus,
    string? FailureCode,
    string? FailureReason,
    Guid RequestedByAccountId,
    string Reason,
    DateTimeOffset RequestedAt);

public sealed record ProductionPackageUpgradeEndpointDetail(
    Guid KioskExecutionEndpointId,
    Guid KioskId,
    Guid SourceConfigurationReleaseId,
    Guid SourceDeploymentId,
    Guid? TargetDeploymentId,
    Guid? RollbackDeploymentId,
    string? RollbackDeploymentStatus,
    string? RollbackFailureCode,
    string? RollbackFailureReason,
    IReadOnlyCollection<ProductionPackageUpgradeRollbackAttemptResult> RollbackAttempts);

public sealed record ProductionPackageUpgradeMenuChangeResult(
    string ChangeKind,
    Guid MenuId,
    Guid MenuItemId,
    Guid BeforeProductId,
    Guid? AfterProductId,
    Guid BeforeProductVariantId,
    Guid? AfterProductVariantId,
    Guid? BeforeRecipeId,
    Guid? AfterRecipeId,
    string BeforeStatus,
    string AfterStatus,
    IReadOnlyCollection<string> OptionSourceKeys);

public sealed record ProductionPackageUpgradeDetailResult(
    ProductionPackageUpgradeResult Summary,
    Guid ApprovedByAccountId,
    DateTimeOffset ApprovedAt,
    Guid? CompletedByAccountId,
    DateTimeOffset? CompletedAt,
    Guid? RollbackRequestedByAccountId,
    DateTimeOffset? RollbackRequestedAt,
    Guid? RolledBackByAccountId,
    DateTimeOffset? RolledBackAt,
    Guid? AbandonedByAccountId,
    DateTimeOffset? AbandonedAt,
    string? AbandonReason,
    IReadOnlyCollection<ProductionPackageUpgradeMenuChangeResult> MenuChanges,
    IReadOnlyCollection<ProductionPackageUpgradeEndpointDetail> Endpoints);

public sealed record ProductionPackageUpgradeResult(
    Guid Id,
    Guid SourceInstallationId,
    Guid TargetPackageVersionId,
    Guid? TargetInstallationId,
    string Status,
    string PreviewChecksum,
    IReadOnlyCollection<string> SelectedProductSourceKeys,
    int MenuChangeCount,
    int EndpointTargetCount,
    string? FailureCode,
    string? FailureMessage)
{
    public static ProductionPackageUpgradeResult From(ProductionPackageUpgrade upgrade) => new(
        upgrade.Id, upgrade.SourceInstallationId, upgrade.TargetPackageVersionId,
        upgrade.TargetInstallationId, upgrade.Status.ToString(), upgrade.PreviewChecksum,
        upgrade.GetSelectedProductSourceKeys(), upgrade.MenuChanges.Count, upgrade.EndpointTargets.Count,
        upgrade.FailureCode, upgrade.FailureMessage);
}

public sealed class ExecuteProductionPackageUpgradeCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid SourceInstallationId { get; init; }
    public Guid TargetPackageVersionId { get; init; }
    public required string PreviewChecksum { get; init; }
    public required string IdempotencyKey { get; init; }
    public IReadOnlyCollection<string> ProductSourceKeys { get; init; } = [];
}
