using System.Text.Json;
using Domain.Common;
using Domain.SalesCatalog.Enums;

namespace Domain.ProductionPackages;

public enum ProductionPackageUpgradeStatus
{
    Materializing = 0,
    ReadyForReview = 1,
    Completed = 2,
    RollbackPending = 3,
    RolledBack = 4,
    Failed = 5,
    Abandoned = 6
}

public enum ProductionPackageUpgradeMenuChangeKind
{
    Rebind = 0,
    DeactivateRemoved = 1
}

public enum ProductionPackageUpgradeAvailabilityResourceKind
{
    Product = 0,
    ProductVariant = 1,
    ProductOption = 2
}

public sealed class ProductionPackageUpgrade : BusinessEntity
{
    private readonly List<ProductionPackageUpgradeMenuChange> _menuChanges = [];
    private readonly List<ProductionPackageUpgradeEndpointTarget> _endpointTargets = [];
    private readonly List<ProductionPackageUpgradeCatalogIdentityChange> _catalogIdentityChanges = [];
    private readonly List<ProductionPackageUpgradeAvailabilityChange> _availabilityChanges = [];

    public Guid OrganizationId { get; private set; }
    public Guid SourceInstallationId { get; private set; }
    public Guid TargetPackageVersionId { get; private set; }
    public Guid? TargetInstallationId { get; private set; }
    public ProductionPackageUpgradeStatus Status { get; private set; }
    public string PreviewChecksum { get; private set; } = null!;
    public string SourceManifestChecksum { get; private set; } = null!;
    public string TargetManifestChecksum { get; private set; } = null!;
    public string SelectedProductSourceKeysJson { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public Guid ApprovedByAccountId { get; private set; }
    public DateTimeOffset ApprovedAt { get; private set; }
    public Guid? CompletedByAccountId { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? RollbackRequestedByAccountId { get; private set; }
    public DateTimeOffset? RollbackRequestedAt { get; private set; }
    public Guid? RolledBackByAccountId { get; private set; }
    public DateTimeOffset? RolledBackAt { get; private set; }
    public DateTimeOffset LastProgressAt => UpdatedAt ?? CreatedAt;
    public Guid? AbandonedByAccountId => Status == ProductionPackageUpgradeStatus.Abandoned
        ? UpdatedByAccountId : null;
    public DateTimeOffset? AbandonedAt => Status == ProductionPackageUpgradeStatus.Abandoned
        ? UpdatedAt : null;
    public string? AbandonReason => Status == ProductionPackageUpgradeStatus.Abandoned
        ? FailureMessage : null;
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    public ProductionPackageInstallation SourceInstallation { get; private set; } = null!;
    public ProductionPackageVersion TargetPackageVersion { get; private set; } = null!;
    public ProductionPackageInstallation? TargetInstallation { get; private set; }
    public IReadOnlyCollection<ProductionPackageUpgradeMenuChange> MenuChanges => _menuChanges;
    public IReadOnlyCollection<ProductionPackageUpgradeEndpointTarget> EndpointTargets => _endpointTargets;
    public IReadOnlyCollection<ProductionPackageUpgradeCatalogIdentityChange> CatalogIdentityChanges => _catalogIdentityChanges;
    public IReadOnlyCollection<ProductionPackageUpgradeAvailabilityChange> AvailabilityChanges => _availabilityChanges;

    private ProductionPackageUpgrade() { }

    public static ProductionPackageUpgrade Approve(
        Guid organizationId,
        Guid sourceInstallationId,
        Guid targetPackageVersionId,
        string previewChecksum,
        string sourceManifestChecksum,
        string targetManifestChecksum,
        IReadOnlyCollection<string> selectedProductSourceKeys,
        string idempotencyKey,
        Guid approvedByAccountId,
        DateTimeOffset approvedAt)
    {
        if (organizationId == Guid.Empty || sourceInstallationId == Guid.Empty ||
            targetPackageVersionId == Guid.Empty || approvedByAccountId == Guid.Empty ||
            selectedProductSourceKeys.Count == 0)
            throw new DomainRuleException("Upgrade organization, installations, selected products, and approver are required.");

        return new ProductionPackageUpgrade
        {
            OrganizationId = organizationId,
            SourceInstallationId = sourceInstallationId,
            TargetPackageVersionId = targetPackageVersionId,
            PreviewChecksum = ProductionPackage.RequireText(previewChecksum, "Upgrade preview checksum"),
            SourceManifestChecksum = ProductionPackage.RequireText(sourceManifestChecksum, "Source manifest checksum"),
            TargetManifestChecksum = ProductionPackage.RequireText(targetManifestChecksum, "Target manifest checksum"),
            SelectedProductSourceKeysJson = JsonSerializer.Serialize(selectedProductSourceKeys
                .Select(key => ProductionPackage.RequireCode(key, "Selected product source key"))
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)),
            IdempotencyKey = ProductionPackage.RequireText(idempotencyKey, "Upgrade idempotency key"),
            ApprovedByAccountId = approvedByAccountId,
            ApprovedAt = approvedAt,
            Status = ProductionPackageUpgradeStatus.Materializing,
            CreatedAt = approvedAt,
            CreatedByAccountId = approvedByAccountId
        };
    }

    public IReadOnlyCollection<string> GetSelectedProductSourceKeys() =>
        JsonSerializer.Deserialize<string[]>(SelectedProductSourceKeysJson) ?? [];

    public void AddMenuChange(ProductionPackageUpgradeMenuChange change)
    {
        EnsureMaterializing();
        if (_menuChanges.Any(item => item.MenuItemId == change.MenuItemId))
            throw new DomainRuleException("A MenuItem can appear only once in upgrade rollback evidence.");
        _menuChanges.Add(change);
    }

    public void AddEndpointTarget(ProductionPackageUpgradeEndpointTarget target)
    {
        EnsureMaterializing();
        if (_endpointTargets.Any(item => item.KioskExecutionEndpointId == target.KioskExecutionEndpointId))
            throw new DomainRuleException("An execution endpoint can appear only once in an upgrade.");
        _endpointTargets.Add(target);
    }

    public void AddCatalogIdentityChange(ProductionPackageUpgradeCatalogIdentityChange change)
    {
        EnsureMaterializing();
        if (_catalogIdentityChanges.Any(item =>
                (change.SourceProductId.HasValue && item.SourceProductId == change.SourceProductId) ||
                                                item.TargetProductId == change.TargetProductId))
            throw new DomainRuleException("A Product identity can appear only once in an upgrade.");
        _catalogIdentityChanges.Add(change);
    }

    public void AddAvailabilityChange(ProductionPackageUpgradeAvailabilityChange change)
    {
        EnsureMaterializing();
        if (_availabilityChanges.Any(item => item.ResourceKind == change.ResourceKind &&
                                             item.SourceResourceId == change.SourceResourceId))
            throw new DomainRuleException("An availability resource can appear only once in an upgrade.");
        _availabilityChanges.Add(change);
    }

    public void MarkReadyForReview(Guid targetInstallationId, DateTimeOffset now)
    {
        EnsureMaterializing();
        if (targetInstallationId == Guid.Empty || _catalogIdentityChanges.Count == 0)
            throw new DomainRuleException("A materialized successor installation and Catalog identity evidence are required.");
        AttachTargetInstallation(targetInstallationId, now);
        Status = ProductionPackageUpgradeStatus.ReadyForReview;
        UpdatedAt = now;
    }

    public void AttachTargetInstallation(Guid targetInstallationId, DateTimeOffset now)
    {
        EnsureMaterializing();
        if (targetInstallationId == Guid.Empty || targetInstallationId == SourceInstallationId)
            throw new DomainRuleException("Successor installation identity is required.");
        if (TargetInstallationId.HasValue && TargetInstallationId != targetInstallationId)
            throw new DomainRuleException("An upgrade cannot replace its successor installation identity.");
        TargetInstallationId = targetInstallationId;
        UpdatedAt = now;
    }

    public void ResumeMaterialization(DateTimeOffset now)
    {
        if (Status != ProductionPackageUpgradeStatus.Failed)
            throw new DomainRuleException("Only a Failed upgrade can resume materialization.");
        FailureCode = null;
        FailureMessage = null;
        Status = ProductionPackageUpgradeStatus.Materializing;
        UpdatedAt = now;
    }

    public void Complete(Guid actorId, DateTimeOffset now)
    {
        if (Status != ProductionPackageUpgradeStatus.ReadyForReview || actorId == Guid.Empty)
            throw new DomainRuleException("Only a reviewed upgrade can complete cutover.");
        Status = ProductionPackageUpgradeStatus.Completed;
        CompletedByAccountId = actorId;
        CompletedAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void BeginRollback(Guid actorId, DateTimeOffset now)
    {
        if (Status == ProductionPackageUpgradeStatus.RollbackPending)
            return;
        if (Status != ProductionPackageUpgradeStatus.Completed || actorId == Guid.Empty)
            throw new DomainRuleException("Only a completed upgrade can begin rollback.");
        Status = ProductionPackageUpgradeStatus.RollbackPending;
        RollbackRequestedByAccountId = actorId;
        RollbackRequestedAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void CompleteRollback(Guid actorId, DateTimeOffset now)
    {
        if (Status != ProductionPackageUpgradeStatus.RollbackPending || actorId == Guid.Empty)
            throw new DomainRuleException("Only a pending rollback can complete.");
        Status = ProductionPackageUpgradeStatus.RolledBack;
        RolledBackByAccountId = actorId;
        RolledBackAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void Fail(string code, string message, DateTimeOffset now)
    {
        if (Status != ProductionPackageUpgradeStatus.Materializing)
            throw new DomainRuleException("Only a materializing upgrade can fail.");
        FailureCode = ProductionPackage.RequireCode(code, "Upgrade failure code");
        FailureMessage = ProductionPackage.RequireText(message, "Upgrade failure message");
        Status = ProductionPackageUpgradeStatus.Failed;
        UpdatedAt = now;
    }

    public void Abandon(Guid actorId, string reason, DateTimeOffset now)
    {
        if (Status == ProductionPackageUpgradeStatus.Abandoned) return;
        if (Status is not ProductionPackageUpgradeStatus.ReadyForReview and
            not ProductionPackageUpgradeStatus.Failed)
            throw new DomainRuleException("Only a ReadyForReview or Failed upgrade can be abandoned.");
        if (actorId == Guid.Empty)
            throw new DomainRuleException("Upgrade abandon actor is required.");
        Status = ProductionPackageUpgradeStatus.Abandoned;
        FailureCode = "UpgradeAbandoned";
        FailureMessage = ProductionPackage.RequireText(reason, "Upgrade abandon reason");
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    private void EnsureMaterializing()
    {
        if (Status != ProductionPackageUpgradeStatus.Materializing)
            throw new DomainRuleException("Upgrade evidence can be recorded only while materializing.");
    }
}

public sealed class ProductionPackageUpgradeAvailabilityChange : BusinessEntity
{
    public Guid UpgradeId { get; private set; }
    public ProductionPackageUpgradeAvailabilityResourceKind ResourceKind { get; private set; }
    public string ResourceSourceKey { get; private set; } = null!;
    public Guid SourceResourceId { get; private set; }
    public Guid TargetResourceId { get; private set; }
    public bool SourceAvailabilityBefore { get; private set; }
    public bool TargetAvailabilityBefore { get; private set; }
    public bool TargetAvailabilityAfter { get; private set; }
    public ProductionPackageUpgrade Upgrade { get; private set; } = null!;
    private ProductionPackageUpgradeAvailabilityChange() { }
    public static ProductionPackageUpgradeAvailabilityChange Create(
        ProductionPackageUpgradeAvailabilityResourceKind kind, string sourceKey,
        Guid sourceId, Guid targetId, bool sourceBefore, bool targetBefore, bool targetAfter)
    {
        if (sourceId == Guid.Empty || targetId == Guid.Empty || sourceId == targetId)
            throw new DomainRuleException("Distinct source and target availability identities are required.");
        return new ProductionPackageUpgradeAvailabilityChange
        {
            ResourceKind = kind,
            ResourceSourceKey = ProductionPackage.RequireCode(sourceKey, "Availability resource source key"),
            SourceResourceId = sourceId,
            TargetResourceId = targetId,
            SourceAvailabilityBefore = sourceBefore,
            TargetAvailabilityBefore = targetBefore,
            TargetAvailabilityAfter = targetAfter
        };
    }
}

public sealed class ProductionPackageUpgradeMenuChange : BusinessEntity
{
    private readonly List<ProductionPackageUpgradeMenuOptionChange> _optionChanges = [];

    public Guid UpgradeId { get; private set; }
    public ProductionPackageUpgradeMenuChangeKind ChangeKind { get; private set; }
    public Guid MenuId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public Guid BeforeProductId { get; private set; }
    public Guid? AfterProductId { get; private set; }
    public Guid BeforeProductVariantId { get; private set; }
    public Guid? AfterProductVariantId { get; private set; }
    public Guid? BeforeRecipeId { get; private set; }
    public Guid? AfterRecipeId { get; private set; }
    public MenuItemStatus BeforeMenuItemStatus { get; private set; }
    public MenuItemStatus AfterMenuItemStatus { get; private set; }
    public string BeforeBindingChecksum { get; private set; } = null!;
    public string AfterBindingChecksum { get; private set; } = null!;
    public ProductionPackageUpgrade Upgrade { get; private set; } = null!;
    public IReadOnlyCollection<ProductionPackageUpgradeMenuOptionChange> OptionChanges => _optionChanges;

    private ProductionPackageUpgradeMenuChange() { }

    public static ProductionPackageUpgradeMenuChange Create(
        ProductionPackageUpgradeMenuChangeKind kind, Guid menuId, Guid menuItemId,
        Guid beforeProductId, Guid? afterProductId, Guid beforeVariantId, Guid? afterVariantId,
        Guid? beforeRecipeId, Guid? afterRecipeId, MenuItemStatus beforeStatus, MenuItemStatus afterStatus,
        string beforeChecksum, string afterChecksum,
        IEnumerable<(string SourceKey, Guid? BeforeOptionId, Guid? AfterOptionId)> options)
    {
        if (menuId == Guid.Empty || menuItemId == Guid.Empty || beforeProductId == Guid.Empty ||
            beforeVariantId == Guid.Empty ||
            (kind == ProductionPackageUpgradeMenuChangeKind.Rebind &&
             (!afterProductId.HasValue || !afterVariantId.HasValue)))
            throw new DomainRuleException("Complete typed MenuItem binding evidence is required.");

        var value = new ProductionPackageUpgradeMenuChange
        {
            ChangeKind = kind,
            MenuId = menuId,
            MenuItemId = menuItemId,
            BeforeProductId = beforeProductId,
            AfterProductId = afterProductId,
            BeforeProductVariantId = beforeVariantId,
            AfterProductVariantId = afterVariantId,
            BeforeRecipeId = beforeRecipeId,
            AfterRecipeId = afterRecipeId,
            BeforeMenuItemStatus = beforeStatus,
            AfterMenuItemStatus = afterStatus,
            BeforeBindingChecksum = ProductionPackage.RequireText(beforeChecksum, "Before binding checksum"),
            AfterBindingChecksum = ProductionPackage.RequireText(afterChecksum, "After binding checksum")
        };
        foreach (var option in options)
            value._optionChanges.Add(ProductionPackageUpgradeMenuOptionChange.Create(
                option.SourceKey, option.BeforeOptionId, option.AfterOptionId));
        if (value._optionChanges.GroupBy(item => item.OptionSourceKey).Any(group => group.Count() > 1))
            throw new DomainRuleException("Upgrade MenuItem option source keys must be unique.");
        return value;
    }
}

public sealed class ProductionPackageUpgradeMenuOptionChange : BusinessEntity
{
    public Guid UpgradeMenuChangeId { get; private set; }
    public string OptionSourceKey { get; private set; } = null!;
    public Guid? BeforeProductOptionId { get; private set; }
    public Guid? AfterProductOptionId { get; private set; }
    public ProductionPackageUpgradeMenuChange UpgradeMenuChange { get; private set; } = null!;
    private ProductionPackageUpgradeMenuOptionChange() { }
    internal static ProductionPackageUpgradeMenuOptionChange Create(string sourceKey, Guid? beforeId, Guid? afterId)
    {
        if (!beforeId.HasValue && !afterId.HasValue)
            throw new DomainRuleException("Menu option evidence requires a before or after identity.");
        return new ProductionPackageUpgradeMenuOptionChange
        {
            OptionSourceKey = ProductionPackage.RequireCode(sourceKey, "Option source key"),
            BeforeProductOptionId = beforeId,
            AfterProductOptionId = afterId
        };
    }
}

public sealed class ProductionPackageUpgradeEndpointTarget : BusinessEntity
{
    private readonly List<ProductionPackageUpgradeRollbackAttempt> _rollbackAttempts = [];

    public Guid UpgradeId { get; private set; }
    public Guid KioskExecutionEndpointId { get; private set; }
    public Guid KioskId { get; private set; }
    public Guid SourceConfigurationReleaseId { get; private set; }
    public Guid SourceDeploymentId { get; private set; }
    public Guid? TargetDeploymentId { get; private set; }
    public Guid? RollbackDeploymentId { get; private set; }
    public ProductionPackageUpgrade Upgrade { get; private set; } = null!;
    public IReadOnlyCollection<ProductionPackageUpgradeRollbackAttempt> RollbackAttempts => _rollbackAttempts;
    private ProductionPackageUpgradeEndpointTarget() { }
    public static ProductionPackageUpgradeEndpointTarget Create(Guid endpointId, Guid kioskId,
        Guid sourceReleaseId, Guid sourceDeploymentId)
    {
        if (endpointId == Guid.Empty || kioskId == Guid.Empty || sourceReleaseId == Guid.Empty || sourceDeploymentId == Guid.Empty)
            throw new DomainRuleException("Upgrade endpoint target requires active source deployment evidence.");
        return new ProductionPackageUpgradeEndpointTarget
        {
            KioskExecutionEndpointId = endpointId,
            KioskId = kioskId,
            SourceConfigurationReleaseId = sourceReleaseId,
            SourceDeploymentId = sourceDeploymentId
        };
    }
    public void RecordTargetDeployment(Guid deploymentId)
    {
        if (deploymentId == Guid.Empty) throw new DomainRuleException("Target deployment id is required.");
        TargetDeploymentId = deploymentId;
    }
    public void RecordRollbackDeployment(Guid deploymentId, Guid actorId, string reason,
        DateTimeOffset now, int maxAttempts)
    {
        if (deploymentId == Guid.Empty || actorId == Guid.Empty)
            throw new DomainRuleException("Rollback deployment and actor identities are required.");
        if (maxAttempts <= 0 || _rollbackAttempts.Count >= maxAttempts)
            throw new DomainRuleException("Rollback deployment retry limit was reached.");
        if (_rollbackAttempts.Any(item => item.DeploymentId == deploymentId))
            throw new DomainRuleException("A rollback deployment can be recorded only once.");
        var attemptNo = _rollbackAttempts.Count == 0 ? 1 : _rollbackAttempts.Max(item => item.AttemptNo) + 1;
        _rollbackAttempts.Add(ProductionPackageUpgradeRollbackAttempt.Create(
            attemptNo, deploymentId, RollbackDeploymentId, actorId, reason, now));
        RollbackDeploymentId = deploymentId;
    }
}

public sealed class ProductionPackageUpgradeRollbackAttempt : BusinessEntity
{
    public Guid UpgradeEndpointTargetId { get; private set; }
    public int AttemptNo { get; private set; }
    public Guid DeploymentId { get; private set; }
    public Guid? ReplacedDeploymentId { get; private set; }
    public Guid RequestedByAccountId { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTimeOffset RequestedAt { get; private set; }
    public ProductionPackageUpgradeEndpointTarget UpgradeEndpointTarget { get; private set; } = null!;

    private ProductionPackageUpgradeRollbackAttempt() { }

    internal static ProductionPackageUpgradeRollbackAttempt Create(int attemptNo, Guid deploymentId,
        Guid? replacedDeploymentId, Guid actorId, string reason, DateTimeOffset now)
    {
        if (attemptNo <= 0 || deploymentId == Guid.Empty || actorId == Guid.Empty)
            throw new DomainRuleException("Complete rollback attempt identity is required.");
        if (replacedDeploymentId == deploymentId)
            throw new DomainRuleException("A rollback retry requires a new deployment identity.");
        return new ProductionPackageUpgradeRollbackAttempt
        {
            AttemptNo = attemptNo,
            DeploymentId = deploymentId,
            ReplacedDeploymentId = replacedDeploymentId,
            RequestedByAccountId = actorId,
            Reason = ProductionPackage.RequireText(reason, "Rollback reason"),
            RequestedAt = now,
            CreatedAt = now,
            CreatedByAccountId = actorId
        };
    }
}

public sealed class ProductionPackageUpgradeCatalogIdentityChange : BusinessEntity
{
    public Guid UpgradeId { get; private set; }
    public string ProductSourceKey { get; private set; } = null!;
    public Guid? SourceProductId { get; private set; }
    public Guid TargetProductId { get; private set; }
    public string SourceCodeBefore { get; private set; } = null!;
    public string SourceCodeAfter { get; private set; } = null!;
    public string TargetCodeBefore { get; private set; } = null!;
    public string TargetCodeAfter { get; private set; } = null!;
    public string BeforeChecksum { get; private set; } = null!;
    public string AfterChecksum { get; private set; } = null!;
    public ProductionPackageUpgrade Upgrade { get; private set; } = null!;
    private ProductionPackageUpgradeCatalogIdentityChange() { }
    public static ProductionPackageUpgradeCatalogIdentityChange Create(string sourceKey,
        Guid? sourceProductId, Guid targetProductId, string? sourceCodeBefore, string? sourceCodeAfter,
        string targetCodeBefore, string targetCodeAfter, string beforeChecksum, string afterChecksum)
    {
        if (targetProductId == Guid.Empty || sourceProductId == targetProductId)
            throw new DomainRuleException("A successor Product identity distinct from its optional source is required.");
        return new ProductionPackageUpgradeCatalogIdentityChange
        {
            ProductSourceKey = ProductionPackage.RequireCode(sourceKey, "Product source key"),
            SourceProductId = sourceProductId,
            TargetProductId = targetProductId,
            SourceCodeBefore = ProductionPackage.NormalizeOptional(sourceCodeBefore) ?? string.Empty,
            SourceCodeAfter = ProductionPackage.NormalizeOptional(sourceCodeAfter) ?? string.Empty,
            TargetCodeBefore = ProductionPackage.RequireCode(targetCodeBefore, "Staging Product code"),
            TargetCodeAfter = ProductionPackage.RequireCode(targetCodeAfter, "Canonical Product code"),
            BeforeChecksum = ProductionPackage.RequireText(beforeChecksum, "Catalog identity before checksum"),
            AfterChecksum = ProductionPackage.RequireText(afterChecksum, "Catalog identity after checksum")
        };
    }
}
