using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Common;

namespace Domain.ProductionPackages;

public enum ProductionPackageStatus { Active = 0, Retired = 1 }
public enum ProductionPackageVersionStatus { Draft = 0, Published = 1, Retired = 2 }
public enum ProductionPackageInstallationStatus { Pending = 0, Materializing = 1, Installed = 2, Failed = 3, Superseded = 4 }
public enum ProductionPackageOwnershipMode { PackageManaged = 0, OrganizationFork = 1 }
public enum ProductionPackageResourceKind
{
    Product = 0, ProductVariant = 1, Recipe = 2, OptionGroup = 3, ProductOption = 4,
    RobotArtifact = 5, RobotProgram = 6, ConfigurationRelease = 7
}
public enum ProductionCompositionStatus { Valid = 0, Invalid = 1, Applied = 2, Superseded = 3 }

public sealed class ProductionPackage : BusinessEntity
{
    private readonly List<ProductionPackageVersion> _versions = [];
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProductionPackageStatus Status { get; private set; }
    public IReadOnlyCollection<ProductionPackageVersion> Versions => _versions;

    private ProductionPackage() { }

    public static ProductionPackage Create(string code, string name, string? description = null) => new()
    {
        Code = RequireCode(code, "Production package code"),
        Name = RequireText(name, "Production package name"),
        Description = NormalizeOptional(description)
    };

    public void Update(string name, string? description)
    {
        if (Status == ProductionPackageStatus.Retired)
            throw new DomainRuleException("Retired production packages cannot be changed.");
        Name = RequireText(name, "Production package name");
        Description = NormalizeOptional(description);
    }

    public void Retire() => Status = ProductionPackageStatus.Retired;

    internal static string RequireCode(string value, string field) => RequireText(value, field).ToUpperInvariant();
    internal static string RequireText(string value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainRuleException($"{field} is required.") : value.Trim();
    internal static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ProductionPackageVersion : BusinessEntity
{
    private readonly List<ProductionPackageProductDefinition> _products = [];
    private readonly List<ProductionPackageArtifactDefinition> _artifacts = [];
    private readonly List<ProductionPackageProgramBlueprint> _programs = [];
    private readonly List<ProductionPackageRouteBlueprint> _routes = [];

    public Guid ProductionPackageId { get; private set; }
    public int Version { get; private set; }
    public ProductionPackageVersionStatus Status { get; private set; }
    public int ManifestSchemaVersion { get; private set; } = 1;
    public string? ManifestJson { get; private set; }
    public string? ManifestChecksum { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? RetiredAt { get; private set; }
    public ProductionPackage ProductionPackage { get; private set; } = null!;
    public IReadOnlyCollection<ProductionPackageProductDefinition> Products => _products;
    public IReadOnlyCollection<ProductionPackageArtifactDefinition> Artifacts => _artifacts;
    public IReadOnlyCollection<ProductionPackageProgramBlueprint> Programs => _programs;
    public IReadOnlyCollection<ProductionPackageRouteBlueprint> Routes => _routes;

    private ProductionPackageVersion() { }

    public static ProductionPackageVersion CreateDraft(Guid packageId, int version)
    {
        if (packageId == Guid.Empty || version <= 0)
            throw new DomainRuleException("Production package and positive version are required.");
        return new ProductionPackageVersion { ProductionPackageId = packageId, Version = version };
    }

    public void ReplaceDefinition(
        IEnumerable<ProductionPackageProductDefinition> products,
        IEnumerable<ProductionPackageArtifactDefinition> artifacts,
        IEnumerable<ProductionPackageProgramBlueprint> programs,
        IEnumerable<ProductionPackageRouteBlueprint> routes)
    {
        EnsureDraft();
        _products.Clear(); _products.AddRange(products);
        _artifacts.Clear(); _artifacts.AddRange(artifacts);
        _programs.Clear(); _programs.AddRange(programs);
        _routes.Clear(); _routes.AddRange(routes);
        ValidateDefinition();
    }

    public void Publish(DateTimeOffset now, Guid? actorId)
    {
        EnsureDraft();
        ValidateDefinition();
        var document = new
        {
            Id,
            ProductionPackageId,
            Version,
            ManifestSchemaVersion,
            Products = _products.OrderBy(x => x.SourceKey).Select(x => x.ToManifest()),
            Artifacts = _artifacts.OrderBy(x => x.SourceKey).Select(x => x.ToManifest()),
            Programs = _programs.OrderBy(x => x.BlueprintCode).Select(x => x.ToManifest()),
            Routes = _routes.OrderBy(x => x.Priority).ThenBy(x => x.RouteCode).Select(x => x.ToManifest())
        };
        ManifestJson = JsonSerializer.Serialize(document);
        ManifestChecksum = Sha256(ManifestJson);
        Status = ProductionPackageVersionStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void Retire(DateTimeOffset now, Guid? actorId)
    {
        if (Status == ProductionPackageVersionStatus.Retired) return;
        if (Status != ProductionPackageVersionStatus.Published)
            throw new DomainRuleException("Only published package versions can be retired.");
        Status = ProductionPackageVersionStatus.Retired;
        RetiredAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    private void ValidateDefinition()
    {
        if (_products.Count == 0 || _artifacts.Count == 0 || _programs.Count == 0 || _routes.Count == 0)
            throw new DomainRuleException("A package version requires products, artifacts, programs, and routes.");
        if (_products.GroupBy(x => x.SourceKey).Any(x => x.Count() > 1) ||
            _products.GroupBy(x => x.SourceProductId).Any(x => x.Count() > 1) ||
            _artifacts.GroupBy(x => x.SourceKey).Any(x => x.Count() > 1) ||
            _programs.GroupBy(x => x.BlueprintCode).Any(x => x.Count() > 1) ||
            _routes.GroupBy(x => x.RouteCode).Any(x => x.Count() > 1))
            throw new DomainRuleException("Package definition product sources and logical keys must be unique.");

        var artifactKeys = _artifacts.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        foreach (var program in _programs)
            program.Validate(artifactKeys);
        var productKeys = _products.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        var programCodes = _programs.Select(x => x.BlueprintCode).ToHashSet(StringComparer.Ordinal);
        if (_routes.Any(x => !productKeys.Contains(x.ProductSourceKey) || !programCodes.Contains(x.ProgramBlueprintCode)))
            throw new DomainRuleException("Package routes must reference package product and program definitions.");
    }

    private void EnsureDraft()
    {
        if (Status != ProductionPackageVersionStatus.Draft)
            throw new DomainRuleException("Only Draft package versions can be changed.");
    }

    internal static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class ProductionPackageProductDefinition : BusinessEntity
{
    public Guid PackageVersionId { get; private set; }
    public string SourceKey { get; private set; } = null!;
    public Guid SourceProductId { get; private set; }
    public string ProductSnapshotJson { get; private set; } = null!;
    public string ProductSnapshotChecksum { get; private set; } = null!;
    public ProductionPackageVersion PackageVersion { get; private set; } = null!;
    private ProductionPackageProductDefinition() { }
    public static ProductionPackageProductDefinition Create(string sourceKey, Guid sourceProductId, string snapshotJson)
    {
        if (sourceProductId == Guid.Empty) throw new DomainRuleException("Package product source id is required.");
        ValidateJson(snapshotJson, "Product snapshot");
        return new ProductionPackageProductDefinition
        {
            SourceKey = ProductionPackage.RequireCode(sourceKey, "Product source key"),
            SourceProductId = sourceProductId,
            ProductSnapshotJson = snapshotJson,
            ProductSnapshotChecksum = ProductionPackageVersion.Sha256(snapshotJson)
        };
    }
    internal object ToManifest() => new { SourceKey, SourceProductId, ProductSnapshot = JsonDocument.Parse(ProductSnapshotJson).RootElement, ProductSnapshotChecksum };
    internal static void ValidateJson(string json, string field)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new DomainRuleException($"{field} is required.");
        try { using var _ = JsonDocument.Parse(json); }
        catch (JsonException ex) { throw new DomainRuleException($"{field} is invalid: {ex.Message}"); }
    }
}

public sealed class ProductionPackageArtifactDefinition : BusinessEntity
{
    public Guid PackageVersionId { get; private set; }
    public string SourceKey { get; private set; } = null!;
    public Guid RobotArtifactTemplateId { get; private set; }
    public string ArtifactChecksum { get; private set; } = null!;
    public Guid TechnicalContractId { get; private set; }
    public string TechnicalContractChecksum { get; private set; } = null!;
    public ProductionPackageVersion PackageVersion { get; private set; } = null!;
    private ProductionPackageArtifactDefinition() { }
    public static ProductionPackageArtifactDefinition Create(string sourceKey, Guid templateId, string artifactChecksum, Guid contractId, string contractChecksum)
    {
        if (templateId == Guid.Empty || contractId == Guid.Empty || string.IsNullOrWhiteSpace(artifactChecksum) || string.IsNullOrWhiteSpace(contractChecksum))
            throw new DomainRuleException("Exact artifact template and technical contract identities are required.");
        return new ProductionPackageArtifactDefinition
        {
            SourceKey = ProductionPackage.RequireCode(sourceKey, "Artifact source key"),
            RobotArtifactTemplateId = templateId,
            ArtifactChecksum = artifactChecksum.Trim(),
            TechnicalContractId = contractId,
            TechnicalContractChecksum = contractChecksum.Trim()
        };
    }
    internal object ToManifest() => new { SourceKey, RobotArtifactTemplateId, ArtifactChecksum, TechnicalContractId, TechnicalContractChecksum };
}

public sealed class ProductionPackageProgramBlueprint : BusinessEntity
{
    private readonly List<ProductionPackageProgramSlot> _slots = [];
    public Guid PackageVersionId { get; private set; }
    public string BlueprintCode { get; private set; } = null!;
    public string RuntimeTargetCode { get; private set; } = null!;
    public string MachineModelCode { get; private set; } = null!;
    public ProductionPackageVersion PackageVersion { get; private set; } = null!;
    public IReadOnlyCollection<ProductionPackageProgramSlot> Slots => _slots;
    private ProductionPackageProgramBlueprint() { }
    public static ProductionPackageProgramBlueprint Create(string code, string runtimeTargetCode, string machineModelCode,
        IEnumerable<(string SlotCode, string ArtifactSourceKey, string RequiredEffectCode, string Phase, bool IsRequired, bool AllowMultiple, int SortHint)> slots)
    {
        var value = new ProductionPackageProgramBlueprint
        {
            BlueprintCode = ProductionPackage.RequireCode(code, "Program blueprint code"),
            RuntimeTargetCode = ProductionPackage.RequireCode(runtimeTargetCode, "Runtime target code"),
            MachineModelCode = ProductionPackage.RequireCode(machineModelCode, "Machine model code")
        };
        value._slots.AddRange(slots.Select(x => ProductionPackageProgramSlot.Create(x.SlotCode, x.ArtifactSourceKey, x.RequiredEffectCode, x.Phase, x.IsRequired, x.AllowMultiple, x.SortHint)));
        return value;
    }
    internal void Validate(IReadOnlySet<string> artifactKeys)
    {
        if (_slots.Count == 0 || _slots.GroupBy(x => x.SlotCode).Any(x => x.Count() > 1))
            throw new DomainRuleException("Program blueprints require unique slots.");
        if (_slots.GroupBy(x => x.RequiredEffectCode).Any(x => x.Count() > 1))
            throw new DomainRuleException("V1 program blueprints require one slot per required effect code.");
        if (_slots.Any(x => !artifactKeys.Contains(x.ArtifactSourceKey)))
            throw new DomainRuleException("Program slots must reference package artifact definitions.");
    }
    internal object ToManifest() => new { BlueprintCode, RuntimeTargetCode, MachineModelCode, Slots = _slots.OrderBy(x => x.SortHint).ThenBy(x => x.SlotCode).Select(x => x.ToManifest()) };
}

public sealed class ProductionPackageProgramSlot : BusinessEntity
{
    public Guid ProgramBlueprintId { get; private set; }
    public string SlotCode { get; private set; } = null!;
    public string ArtifactSourceKey { get; private set; } = null!;
    public string RequiredEffectCode { get; private set; } = null!;
    public string Phase { get; private set; } = null!;
    public bool IsRequired { get; private set; }
    public bool AllowMultiple { get; private set; }
    public int SortHint { get; private set; }
    public ProductionPackageProgramBlueprint ProgramBlueprint { get; private set; } = null!;
    private ProductionPackageProgramSlot() { }
    internal static ProductionPackageProgramSlot Create(string slotCode, string artifactSourceKey, string effectCode, string phase, bool required, bool multiple, int sortHint) => new()
    {
        SlotCode = ProductionPackage.RequireCode(slotCode, "Program slot code"),
        ArtifactSourceKey = ProductionPackage.RequireCode(artifactSourceKey, "Artifact source key"),
        RequiredEffectCode = ProductionPackage.RequireCode(effectCode, "Required effect code"),
        Phase = ProductionPackage.RequireCode(phase, "Execution phase"),
        IsRequired = required,
        AllowMultiple = multiple,
        SortHint = sortHint
    };
    internal object ToManifest() => new { SlotCode, ArtifactSourceKey, RequiredEffectCode, Phase, IsRequired, AllowMultiple, SortHint };
}

public sealed class ProductionPackageRouteBlueprint : BusinessEntity
{
    public Guid PackageVersionId { get; private set; }
    public string RouteCode { get; private set; } = null!;
    public string ProductSourceKey { get; private set; } = null!;
    public string ProductVariantSourceKey { get; private set; } = null!;
    public string RecipeSourceKey { get; private set; } = null!;
    public string SupportedOptionCodesJson { get; private set; } = "[]";
    public string ProgramBlueprintCode { get; private set; } = null!;
    public string RequiredCapabilitiesJson { get; private set; } = null!;
    public int Priority { get; private set; }
    public ProductionPackageVersion PackageVersion { get; private set; } = null!;
    private ProductionPackageRouteBlueprint() { }
    public static ProductionPackageRouteBlueprint Create(string routeCode, string productSourceKey,
        string productVariantSourceKey, string recipeSourceKey, IReadOnlyCollection<string> supportedOptionCodes,
        string programCode, string capabilitiesJson, int priority)
    {
        ProductionPackageProductDefinition.ValidateJson(capabilitiesJson, "Required capabilities");
        if (priority < 0) throw new DomainRuleException("Route priority cannot be negative.");
        return new ProductionPackageRouteBlueprint
        {
            RouteCode = ProductionPackage.RequireCode(routeCode, "Route code"),
            ProductSourceKey = ProductionPackage.RequireCode(productSourceKey, "Product source key"),
            ProductVariantSourceKey = ProductionPackage.RequireCode(productVariantSourceKey, "Product variant source key"),
            RecipeSourceKey = ProductionPackage.RequireCode(recipeSourceKey, "Recipe source key"),
            SupportedOptionCodesJson = JsonSerializer.Serialize(supportedOptionCodes
                .Select(code => ProductionPackage.RequireCode(code, "Supported option code"))
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()),
            ProgramBlueprintCode = ProductionPackage.RequireCode(programCode, "Program blueprint code"),
            RequiredCapabilitiesJson = capabilitiesJson,
            Priority = priority
        };
    }
    public IReadOnlyCollection<string> GetSupportedOptionCodes() =>
        JsonSerializer.Deserialize<string[]>(SupportedOptionCodesJson) ?? [];
    internal object ToManifest() => new { RouteCode, ProductSourceKey, ProductVariantSourceKey, RecipeSourceKey,
        SupportedOptionCodes = GetSupportedOptionCodes(), ProgramBlueprintCode,
        RequiredCapabilities = JsonDocument.Parse(RequiredCapabilitiesJson).RootElement, Priority };
}

public sealed class ProductionPackageInstallation : BusinessEntity
{
    private readonly List<ProductionPackageMaterialization> _materializations = [];
    public Guid OrganizationId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? KioskId { get; private set; }
    public Guid PackageVersionId { get; private set; }
    public string PackageManifestChecksum { get; private set; } = null!;
    public string RequestChecksum { get; private set; } = null!;
    public string SelectedProductSourceKeysJson { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public ProductionPackageInstallationStatus Status { get; private set; }
    public ProductionPackageOwnershipMode OwnershipMode { get; private set; }
    public Guid? DraftConfigurationReleaseId { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public ProductionPackageVersion PackageVersion { get; private set; } = null!;
    public IReadOnlyCollection<ProductionPackageMaterialization> Materializations => _materializations;
    private ProductionPackageInstallation() { }
    public static ProductionPackageInstallation Start(Guid organizationId, Guid? storeId, Guid? kioskId,
        Guid versionId, string manifestChecksum, string requestChecksum, string idempotencyKey,
        IReadOnlyCollection<string> selectedProductSourceKeys, DateTimeOffset now)
    {
        if (organizationId == Guid.Empty || versionId == Guid.Empty || string.IsNullOrWhiteSpace(manifestChecksum) ||
            string.IsNullOrWhiteSpace(requestChecksum) || string.IsNullOrWhiteSpace(idempotencyKey) ||
            selectedProductSourceKeys.Count == 0)
            throw new DomainRuleException("Installation scope, package version, request checksum, and idempotency key are required.");
        return new ProductionPackageInstallation
        {
            OrganizationId = organizationId, StoreId = storeId, KioskId = kioskId,
            PackageVersionId = versionId, PackageManifestChecksum = manifestChecksum.Trim(),
            RequestChecksum = requestChecksum.Trim(), IdempotencyKey = idempotencyKey.Trim(),
            SelectedProductSourceKeysJson = JsonSerializer.Serialize(selectedProductSourceKeys
                .Select(x => ProductionPackage.RequireCode(x, "Selected product source key"))
                .OrderBy(x => x, StringComparer.Ordinal).Distinct().ToArray()),
            StartedAt = now
        };
    }

    public IReadOnlyCollection<string> GetSelectedProductSourceKeys()
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(SelectedProductSourceKeysJson) ?? [];
        }
        catch (JsonException ex)
        {
            throw new DomainRuleException($"Installation selected-product snapshot is invalid: {ex.Message}");
        }
    }
    public void MarkMaterializing() { if (Status != ProductionPackageInstallationStatus.Pending) throw new DomainRuleException("Only Pending installations can materialize."); Status = ProductionPackageInstallationStatus.Materializing; }
    public void AddMaterialization(ProductionPackageResourceKind kind, string sourceKey, string targetKey, string? checksum = null)
    {
        if (Status != ProductionPackageInstallationStatus.Materializing) throw new DomainRuleException("Installation must be materializing before resources are recorded.");
        if (_materializations.Any(x => x.ResourceKind == kind && x.SourceKey == sourceKey)) throw new DomainRuleException("Package resource has already been materialized.");
        _materializations.Add(ProductionPackageMaterialization.Create(kind, sourceKey, targetKey, checksum));
    }
    public void Complete(Guid releaseId, DateTimeOffset now) { if (Status != ProductionPackageInstallationStatus.Materializing || releaseId == Guid.Empty) throw new DomainRuleException("Materializing installation and Draft release are required."); DraftConfigurationReleaseId = releaseId; Status = ProductionPackageInstallationStatus.Installed; CompletedAt = now; }
    public void Fail(string code, string message, DateTimeOffset now)
    {
        _materializations.Clear();
        FailureCode = ProductionPackage.RequireCode(code, "Installation failure code");
        FailureMessage = ProductionPackage.RequireText(message, "Installation failure message");
        Status = ProductionPackageInstallationStatus.Failed;
        CompletedAt = now;
    }
    public void Restart(DateTimeOffset now)
    {
        if (Status != ProductionPackageInstallationStatus.Failed)
            throw new DomainRuleException("Only Failed package installations can restart.");
        _materializations.Clear();
        FailureCode = null;
        FailureMessage = null;
        CompletedAt = null;
        StartedAt = now;
        Status = ProductionPackageInstallationStatus.Pending;
    }
    public void Fork() { if (Status != ProductionPackageInstallationStatus.Installed) throw new DomainRuleException("Only Installed package configurations can be forked."); OwnershipMode = ProductionPackageOwnershipMode.OrganizationFork; }
}

public sealed class ProductionPackageMaterialization : BusinessEntity
{
    public Guid InstallationId { get; private set; }
    public ProductionPackageResourceKind ResourceKind { get; private set; }
    public string SourceKey { get; private set; } = null!;
    public string TargetKey { get; private set; } = null!;
    public string? TargetChecksum { get; private set; }
    public ProductionPackageInstallation Installation { get; private set; } = null!;
    private ProductionPackageMaterialization() { }
    internal static ProductionPackageMaterialization Create(ProductionPackageResourceKind kind, string sourceKey, string targetKey, string? checksum) => new()
    {
        ResourceKind = kind, SourceKey = ProductionPackage.RequireCode(sourceKey, "Materialization source key"),
        TargetKey = ProductionPackage.RequireText(targetKey, "Materialization target key"), TargetChecksum = ProductionPackage.NormalizeOptional(checksum)
    };
}

public sealed class ProductionComposition : BusinessEntity
{
    public Guid InstallationId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public Guid RecipeId { get; private set; }
    public Guid? TargetExecutionEndpointId { get; private set; }
    public string RuntimeTargetCode { get; private set; } = null!;
    public string MachineModelCode { get; private set; } = null!;
    public int InputSchemaVersion { get; private set; } = 1;
    public string InputJson { get; private set; } = null!;
    public string InputChecksum { get; private set; } = null!;
    public ProductionCompositionStatus Status { get; private set; }
    public Guid? GeneratedRobotProgramId { get; private set; }
    public string ReportJson { get; private set; } = null!;
    private ProductionComposition() { }
    public static ProductionComposition Create(Guid installationId, Guid organizationId, Guid variantId, Guid recipeId, Guid? endpointId, string runtimeTarget, string machineModel, string inputJson, bool valid, string reportJson)
    {
        ProductionPackageProductDefinition.ValidateJson(inputJson, "Composition input");
        ProductionPackageProductDefinition.ValidateJson(reportJson, "Composition report");
        return new ProductionComposition
        {
            InstallationId = installationId, OrganizationId = organizationId, ProductVariantId = variantId,
            RecipeId = recipeId, TargetExecutionEndpointId = endpointId,
            RuntimeTargetCode = ProductionPackage.RequireCode(runtimeTarget, "Runtime target"),
            MachineModelCode = ProductionPackage.RequireCode(machineModel, "Machine model"),
            InputJson = inputJson, InputChecksum = ProductionPackageVersion.Sha256(inputJson),
            Status = valid ? ProductionCompositionStatus.Valid : ProductionCompositionStatus.Invalid,
            ReportJson = reportJson
        };
    }
    public void Apply(Guid programId) { if (Status != ProductionCompositionStatus.Valid || programId == Guid.Empty) throw new DomainRuleException("Only Valid composition can be applied to a RobotProgram."); GeneratedRobotProgramId = programId; Status = ProductionCompositionStatus.Applied; }
    public void Supersede() { if (Status != ProductionCompositionStatus.Superseded) Status = ProductionCompositionStatus.Superseded; }
}
