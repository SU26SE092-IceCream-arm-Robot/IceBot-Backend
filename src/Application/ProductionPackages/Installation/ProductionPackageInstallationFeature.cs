using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.ProductionConfiguration.Routes.Support;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;

namespace Application.ProductionPackages.Installation;

public interface IProductionPackageInstallationStore
{
    Task<bool> ScopeExistsAsync(Guid organizationId, Guid? storeId, Guid? kioskId, CancellationToken cancellationToken);
    Task<ProductionPackageInstallation?> FindByIdempotencyKeyAsync(Guid organizationId, string key, CancellationToken cancellationToken);
    Task<ProductionPackageInstallation?> GetAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken);
    Task<ProductionPackageInstallation?> GetForEditAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid organizationId, ProductionPackageInstallationStatus? status, Guid? storeId,
        Guid? kioskId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionPackageInstallation>> ListAsync(Guid organizationId,
        ProductionPackageInstallationStatus? status, Guid? storeId, Guid? kioskId, int pageNumber, int pageSize,
        CancellationToken cancellationToken);
    Task<ProductionPackageInstallationInsertResult> InsertOrGetAsync(ProductionPackageInstallation installation, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid organizationId, Guid installationId, string failureCode,
        string failureMessage, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<ConfigurationRelease> PersistMaterializedGraphAsync(
        ProductionPackageInstallation installation,
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<RobotArtifact> artifacts,
        IReadOnlyCollection<RobotProgram> programs,
        IReadOnlyCollection<ProductionComposition> compositions,
        Func<long, ConfigurationRelease> releaseFactory,
        CancellationToken cancellationToken);
}

public sealed record ProductionPackageInstallationInsertResult(bool Created, ProductionPackageInstallation Installation);

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
        installation.PackageVersionId,
        installation.Status.ToString(), installation.OwnershipMode.ToString(),
        installation.DraftConfigurationReleaseId, installation.FailureCode, installation.FailureMessage,
        installation.Materializations.Select(x => new ProductionPackageMaterializationResult(
            x.ResourceKind.ToString(), x.SourceKey, x.TargetKey, x.TargetChecksum)).ToArray());
}

public sealed record ProductionPackageMaterializationResult(
    string ResourceKind, string SourceKey, string TargetKey, string? TargetChecksum);

public sealed record ProductionPackageInstallationPreview(
    Guid PackageVersionId,
    string ManifestChecksum,
    IReadOnlyCollection<string> ProductSourceKeys,
    IReadOnlyCollection<string> ProgramBlueprintCodes,
    IReadOnlyCollection<string> RouteCodes,
    IReadOnlyCollection<string> Warnings);

public sealed class ProductionPackageInstallationService(
    IProductionPackageStore packages,
    IProductionPackageInstallationStore installations,
    IArtifactObjectStorage objectStorage,
    ArtifactUploadContentService contentService)
{
    public async Task<ApiResult<ProductionPackageInstallationPreview>> PreviewAsync(
        CurrentUserContext user, Guid organizationId, Guid packageId, Guid versionId,
        IReadOnlyCollection<string> selectedProductSourceKeys, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId, null, null))
            return ApiResult<ProductionPackageInstallationPreview>.Fail("Access denied.", 403);
        var version = await packages.GetVersionAsync(packageId, versionId, false, cancellationToken);
        if (version is null || version.Status != ProductionPackageVersionStatus.Published || string.IsNullOrWhiteSpace(version.ManifestChecksum))
            return ApiResult<ProductionPackageInstallationPreview>.Fail("Published package version not found.", 404);
        var selected = selectedProductSourceKeys.Count == 0
            ? version.Products.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal)
            : selectedProductSourceKeys.Select(x => x.Trim().ToUpperInvariant()).ToHashSet(StringComparer.Ordinal);
        if (selected.Any(x => version.Products.All(product => product.SourceKey != x)))
            return ApiResult<ProductionPackageInstallationPreview>.Fail("One or more selected package products do not exist.", 400);
        try
        {
            var contracts = await packages.LoadTechnicalContractsAsync(
                version.Artifacts.Select(x => x.TechnicalContractId).Distinct().ToArray(), cancellationToken);
            ProductionPackageDefinitionValidator.Validate(version, contracts);
            return ApiResult<ProductionPackageInstallationPreview>.Success(new ProductionPackageInstallationPreview(
                version.Id, version.ManifestChecksum, selected.Order().ToArray(),
                version.Programs.Select(x => x.BlueprintCode).Order().ToArray(),
                version.Routes.Where(x => selected.Contains(x.ProductSourceKey)).Select(x => x.RouteCode).Order().ToArray(),
                ["Package installation creates Draft technical configuration and requires review before publication."]));
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ProductionPackageInstallationPreview>.Fail(ex.Message, 409);
        }
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> InstallAsync(
        InstallProductionPackageCommand command, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, command.UserContext,
                command.OrganizationId, command.StoreId, command.KioskId))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Idempotency-Key is required.", 400);
        if (!await installations.ScopeExistsAsync(command.OrganizationId, command.StoreId, command.KioskId, cancellationToken))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Package installation scope was not found.", 404);

        var version = await packages.GetVersionAsync(command.PackageId, command.PackageVersionId, false, cancellationToken);
        if (version is null || version.Status != ProductionPackageVersionStatus.Published || string.IsNullOrWhiteSpace(version.ManifestChecksum))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Published production package version not found.", 404);

        var selectedKeys = command.ProductSourceKeys.Count == 0
            ? version.Products.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal)
            : command.ProductSourceKeys.Select(x => x.Trim().ToUpperInvariant()).ToHashSet(StringComparer.Ordinal);
        if (selectedKeys.Any(x => version.Products.All(product => product.SourceKey != x)))
            return ApiResult<ProductionPackageInstallationResult>.Fail("One or more selected package products do not exist.", 400);
        var requestChecksum = ComputeRequestChecksum(command, version, selectedKeys);
        var validationContracts = await packages.LoadTechnicalContractsAsync(
            version.Artifacts.Select(x => x.TechnicalContractId).Distinct().ToArray(), cancellationToken);
        try { ProductionPackageDefinitionValidator.Validate(version, validationContracts); }
        catch (DomainRuleException ex) { return ApiResult<ProductionPackageInstallationResult>.Fail(ex.Message, 409); }

        var existing = await installations.FindByIdempotencyKeyAsync(command.OrganizationId, command.IdempotencyKey.Trim(), cancellationToken);
        ProductionPackageInstallation installation;
        if (existing is not null)
        {
            if (existing.PackageVersionId != version.Id || existing.PackageManifestChecksum != version.ManifestChecksum ||
                existing.RequestChecksum != requestChecksum)
                return ApiResult<ProductionPackageInstallationResult>.Fail("Idempotency key was already used with a different installation payload.", 409);
            if (existing.Status != ProductionPackageInstallationStatus.Failed)
                return ApiResult<ProductionPackageInstallationResult>.Success(ProductionPackageInstallationResult.From(existing),
                    "Existing package installation returned.");
            existing.Restart(DateTimeOffset.UtcNow);
            installation = existing;
            await installations.SaveChangesAsync(cancellationToken);
        }
        else
        {
            installation = ProductionPackageInstallation.Start(command.OrganizationId, command.StoreId, command.KioskId,
                version.Id, version.ManifestChecksum, requestChecksum, command.IdempotencyKey,
                selectedKeys.ToArray(), DateTimeOffset.UtcNow);
            installation.CreatedByAccountId = command.UserContext.AccountId;
            var inserted = await installations.InsertOrGetAsync(installation, cancellationToken);
            if (!inserted.Created)
            {
                var winner = inserted.Installation;
                if (winner.PackageVersionId != version.Id || winner.PackageManifestChecksum != version.ManifestChecksum ||
                    winner.RequestChecksum != requestChecksum)
                    return ApiResult<ProductionPackageInstallationResult>.Fail(
                        "Idempotency key was concurrently used with a different installation payload.", 409);
                return ApiResult<ProductionPackageInstallationResult>.Success(
                    ProductionPackageInstallationResult.From(winner), "Concurrent package installation returned.");
            }
        }
        installation.MarkMaterializing();
        await installations.SaveChangesAsync(cancellationToken);

        var copiedKeys = new List<string>();
        try
        {
            var productDefinitions = version.Products.Where(x => selectedKeys.Contains(x.SourceKey)).ToArray();
            if (productDefinitions.Length != selectedKeys.Count)
                throw new DomainRuleException("One or more selected package products do not exist.");

            var optionImpacts = ProductionPackageDefinitionValidator.ResolveOptionExecutionImpacts(
                version, validationContracts);
            var products = MaterializeProducts(command, installation, productDefinitions, optionImpacts);
            var templates = await packages.LoadArtifactTemplatesAsync(version.Artifacts.Select(x => x.RobotArtifactTemplateId).ToArray(), cancellationToken);
            var contracts = await packages.LoadTechnicalContractsAsync(version.Artifacts.Select(x => x.TechnicalContractId).ToArray(), cancellationToken);
            var artifacts = await MaterializeArtifactsAsync(command, installation, version, templates, contracts, copiedKeys, cancellationToken);
            var programsAndCompositions = ComposePrograms(command, installation, version, products, artifacts,
                contracts, optionImpacts);

            var release = await installations.PersistMaterializedGraphAsync(
                installation, products.Select(x => x.Product).ToArray(), artifacts.Values.ToArray(),
                programsAndCompositions.Programs.Values.ToArray(), programsAndCompositions.Compositions,
                releaseNumber => CreateRelease(command, installation, version, releaseNumber, products,
                    programsAndCompositions.Programs, optionImpacts), cancellationToken);
            return ApiResult<ProductionPackageInstallationResult>.Success(
                ProductionPackageInstallationResult.From(installation), "Production package installed as Draft configuration.", 201);
        }
        catch (Exception ex) when (ex is DomainRuleException or ArtifactObjectNotFoundException or
                                   ArtifactObjectIntegrityException or ArtifactObjectStorageUnavailableException or
                                   Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            foreach (var key in copiedKeys) await contentService.DeleteUncommittedObjectAsync(key);
            await installations.MarkFailedAsync(command.OrganizationId, installation.Id,
                "PackageMaterializationFailed", ex.Message, CancellationToken.None);
            return ApiResult<ProductionPackageInstallationResult>.Fail(ex.Message, ex is ArtifactObjectStorageUnavailableException ? 503 : 409);
        }
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> GetAsync(CurrentUserContext user,
        Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId, null, null))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        var installation = await installations.GetAsync(organizationId, installationId, cancellationToken);
        return installation is null
            ? ApiResult<ProductionPackageInstallationResult>.Fail("Package installation not found.", 404)
            : ApiResult<ProductionPackageInstallationResult>.Success(ProductionPackageInstallationResult.From(installation));
    }

    public async Task<PagedResult<ProductionPackageInstallationResult>> ListAsync(CurrentUserContext user,
        Guid organizationId, string? status, Guid? storeId, Guid? kioskId, int pageNumber, int pageSize,
        CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId, storeId, kioskId))
            return PagedResult<ProductionPackageInstallationResult>.Forbidden("Access denied.", pageNumber, pageSize);
        ProductionPackageInstallationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProductionPackageInstallationStatus>(status, true, out var value))
                return PagedResult<ProductionPackageInstallationResult>.Fail(
                    "Invalid production package installation status.", 400, pageNumber, pageSize);
            parsedStatus = value;
        }
        var total = await installations.CountAsync(organizationId, parsedStatus, storeId, kioskId, cancellationToken);
        var rows = await installations.ListAsync(organizationId, parsedStatus, storeId, kioskId,
            pageNumber, pageSize, cancellationToken);
        return PagedResult<ProductionPackageInstallationResult>.Success(
            rows.Select(ProductionPackageInstallationResult.From), total, pageNumber, pageSize);
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> RetryAsync(CurrentUserContext user,
        Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        var installation = await installations.GetForEditAsync(organizationId, installationId, cancellationToken);
        if (installation is null)
            return ApiResult<ProductionPackageInstallationResult>.Fail("Package installation not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, user, organizationId,
                installation.StoreId, installation.KioskId))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        if (installation.Status != ProductionPackageInstallationStatus.Failed)
            return ApiResult<ProductionPackageInstallationResult>.Fail("Only a Failed installation can be retried.", 409);

        return await InstallAsync(new InstallProductionPackageCommand
        {
            UserContext = user,
            OrganizationId = organizationId,
            StoreId = installation.StoreId,
            KioskId = installation.KioskId,
            PackageId = installation.PackageVersion.ProductionPackageId,
            PackageVersionId = installation.PackageVersionId,
            IdempotencyKey = installation.IdempotencyKey,
            ProductSourceKeys = installation.GetSelectedProductSourceKeys()
        }, cancellationToken);
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> ForkAsync(CurrentUserContext user,
        Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageFork, user, organizationId, null, null))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        var installation = await installations.GetForEditAsync(organizationId, installationId, cancellationToken);
        if (installation is null) return ApiResult<ProductionPackageInstallationResult>.Fail("Package installation not found.", 404);
        try
        {
            installation.Fork();
            installation.UpdatedByAccountId = user.AccountId;
            await installations.SaveChangesAsync(cancellationToken);
            return ApiResult<ProductionPackageInstallationResult>.Success(ProductionPackageInstallationResult.From(installation),
                "Package-managed configuration converted to an organization fork.");
        }
        catch (DomainRuleException ex) { return ApiResult<ProductionPackageInstallationResult>.Fail(ex.Message, 409); }
    }

    private static IReadOnlyCollection<MaterializedProduct> MaterializeProducts(
        InstallProductionPackageCommand command, ProductionPackageInstallation installation,
        IReadOnlyCollection<ProductionPackageProductDefinition> definitions,
        IReadOnlyDictionary<Guid, ProductOptionExecutionImpact> optionImpacts)
    {
        var now = DateTimeOffset.UtcNow;
        var scopeType = TenantScopeResolver.Resolve(command.StoreId, command.KioskId);
        var result = new List<MaterializedProduct>();
        foreach (var definition in definitions)
        {
            var source = ProductionPackageProductSnapshotCodec.Deserialize(definition.ProductSnapshotJson).Product;
            var product = new Product
            {
                OrganizationId = command.OrganizationId, StoreId = command.StoreId, KioskId = command.KioskId,
                TemplateProductId = source.Id, CategoryId = source.CategoryId, Code = source.Code, Name = source.Name,
                DisplayName = source.DisplayName, Description = source.Description, ProductType = source.ProductType,
                BasePrice = source.BasePrice, Currency = source.Currency, IsAvailable = false,
                PreparationTimeSeconds = source.PreparationTimeSeconds, ImageUrl = source.ImageUrl,
                ScopeType = scopeType, CreatedAt = now, CreatedByAccountId = command.UserContext.AccountId
            };
            var variants = new Dictionary<Guid, ProductVariant>();
            var recipesByCode = new Dictionary<string, Recipe>(StringComparer.Ordinal);
            var recipeRequirements = new Dictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>>(StringComparer.Ordinal);
            var optionRequirementsByCode = new Dictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>>(
                StringComparer.Ordinal);
            foreach (var variantSource in source.Variants)
            {
                var variant = new ProductVariant
                {
                    ProductId = product.Id, Code = variantSource.Code, Name = variantSource.Name,
                    DisplayName = variantSource.DisplayName, Description = variantSource.Description,
                    VariantType = variantSource.VariantType, FulfillmentType = variantSource.FulfillmentType,
                    SizeCode = variantSource.SizeCode, BasePrice = variantSource.BasePrice, Currency = source.Currency,
                    IsAvailable = false, DisplayOrder = variantSource.DisplayOrder,
                    PreparationTimeSeconds = variantSource.PreparationTimeSeconds, ImageUrl = variantSource.ImageUrl,
                    CreatedAt = now, CreatedByAccountId = command.UserContext.AccountId
                };
                product.ProductVariants.Add(variant);
                variants[variantSource.Id] = variant;
                foreach (var recipeSource in variantSource.Recipes)
                {
                    var currentRecipeRequirements = new List<IngredientQuantityRequirement>();
                    var recipe = new Recipe
                    {
                        OrganizationId = command.OrganizationId, StoreId = command.StoreId, KioskId = command.KioskId,
                        ProductVariantId = variant.Id, TemplateRecipeId = recipeSource.Id, Code = recipeSource.Code,
                        Name = recipeSource.Name, Version = 1, Status = RecipeStatus.Draft,
                        IsDefault = recipeSource.IsDefault, YieldQuantity = recipeSource.YieldQuantity, Unit = recipeSource.Unit,
                        EstimatedDurationSeconds = recipeSource.EstimatedDurationSeconds, EffectiveFrom = recipeSource.EffectiveFrom,
                        EffectiveTo = recipeSource.EffectiveTo, InstructionsSchemaVersion = recipeSource.InstructionsSchemaVersion,
                        InstructionsJson = recipeSource.InstructionsJson, ScopeType = scopeType,
                        CreatedAt = now, CreatedByAccountId = command.UserContext.AccountId
                    };
                    foreach (var item in recipeSource.Items)
                    {
                        recipe.RecipeItems.Add(new RecipeItem { RecipeId = recipe.Id, IngredientId = item.IngredientId,
                            Quantity = item.Quantity, Unit = item.Unit, StepOrder = item.StepOrder, IsOptional = item.IsOptional,
                            Notes = item.Notes, CreatedAt = now, CreatedByAccountId = command.UserContext.AccountId });
                        currentRecipeRequirements.Add(new IngredientQuantityRequirement(item.IngredientCode, item.Quantity, item.Unit, null));
                    }
                    variant.Recipes.Add(recipe);
                    var recipeKey = RecipeLookupKey(variant.Code, recipe.Code);
                    if (!recipesByCode.TryAdd(recipeKey, recipe))
                        throw new DomainRuleException("Package Product snapshot contains duplicate Recipe codes within one variant.");
                    recipeRequirements.Add(recipeKey, currentRecipeRequirements);
                    installation.AddMaterialization(ProductionPackageResourceKind.Recipe,
                        $"{definition.SourceKey}:RECIPE:{recipeSource.Code}", recipe.Id.ToString("D"));
                }
                installation.AddMaterialization(ProductionPackageResourceKind.ProductVariant,
                    $"{definition.SourceKey}:VARIANT:{variantSource.Code}", variant.Id.ToString("D"));
            }

            foreach (var groupSource in source.OptionGroups)
            {
                var group = new OptionGroup { ProductId = product.Id, Code = groupSource.Code, Name = groupSource.Name,
                    Description = groupSource.Description, SelectionType = groupSource.SelectionType,
                    MinSelections = groupSource.MinSelections, MaxSelections = groupSource.MaxSelections,
                    IsRequired = groupSource.IsRequired, IsActive = groupSource.IsActive,
                    DisplayOrder = groupSource.DisplayOrder, CreatedAt = now, CreatedByAccountId = command.UserContext.AccountId };
                foreach (var optionSource in groupSource.Options)
                {
                    var currentOptionRequirements = new List<IngredientQuantityRequirement>();
                    var option = new ProductOption { OptionGroupId = group.Id, TemplateProductOptionId = optionSource.Id,
                        Code = optionSource.Code, Name = optionSource.Name, Description = optionSource.Description,
                        PriceDelta = optionSource.PriceDelta, ExecutionImpact = optionImpacts[optionSource.Id],
                        IsDefault = optionSource.IsDefault, IsAvailable = false,
                        DisplayOrder = optionSource.DisplayOrder, CreatedAt = now, CreatedByAccountId = command.UserContext.AccountId };
                    foreach (var requirement in optionSource.IngredientRequirements)
                    {
                        option.IngredientRequirements.Add(new ProductOptionIngredientRequirement
                        {
                            IngredientId = requirement.IngredientId, Quantity = requirement.Quantity, Unit = requirement.Unit,
                            RequiredWorkcellCapabilityCode = requirement.RequiredWorkcellCapabilityCode,
                            CreatedAt = now, CreatedByAccountId = command.UserContext.AccountId
                        });
                        currentOptionRequirements.Add(new IngredientQuantityRequirement(
                            requirement.IngredientCode, requirement.Quantity, requirement.Unit,
                            optionSource.Code.Trim().ToUpperInvariant()));
                    }
                    optionRequirementsByCode.Add(optionSource.Code.Trim().ToUpperInvariant(), currentOptionRequirements);
                    group.ProductOptions.Add(option);
                    installation.AddMaterialization(ProductionPackageResourceKind.ProductOption,
                        $"{definition.SourceKey}:OPTION:{optionSource.Code}", option.Id.ToString("D"));
                }
                product.OptionGroups.Add(group);
            }
            installation.AddMaterialization(ProductionPackageResourceKind.Product, definition.SourceKey, product.Id.ToString("D"));
            result.Add(new MaterializedProduct(definition.SourceKey, product, recipesByCode,
                recipeRequirements, optionRequirementsByCode));
        }
        return result;
    }

    private async Task<Dictionary<string, RobotArtifact>> MaterializeArtifactsAsync(
        InstallProductionPackageCommand command, ProductionPackageInstallation installation,
        ProductionPackageVersion version, IReadOnlyCollection<RobotArtifactTemplate> templates,
        IReadOnlyCollection<RobotArtifactTechnicalContract> contracts, ICollection<string> copiedKeys,
        CancellationToken cancellationToken)
    {
        var templatesById = templates.ToDictionary(x => x.Id);
        var contractsById = contracts.ToDictionary(x => x.Id);
        var result = new Dictionary<string, RobotArtifact>(StringComparer.Ordinal);
        foreach (var definition in version.Artifacts)
        {
            if (!templatesById.TryGetValue(definition.RobotArtifactTemplateId, out var template) ||
                template.Status != RobotArtifactStatus.Published || template.Checksum != definition.ArtifactChecksum ||
                !contractsById.TryGetValue(definition.TechnicalContractId, out var contract) ||
                contract.Status != RobotArtifactContractStatus.Published || contract.ContractChecksum != definition.TechnicalContractChecksum)
                throw new DomainRuleException("Package artifact source no longer matches its immutable definition.");

            var artifactId = Guid.NewGuid();
            var destination = $"robot-artifacts/{command.OrganizationId:D}/{artifactId:D}/{template.Checksum}.lua";
            await objectStorage.CopyImmutableAsync(template.StorageKey,
                new ArtifactObjectWriteRequest(destination, "application/octet-stream", template.ContentLengthBytes, template.Checksum), cancellationToken);
            copiedKeys.Add(destination);
            var artifact = RobotArtifact.CreateDraft(command.OrganizationId, definition.SourceKey,
                template.TemplateName, destination, template.FileName, template.Checksum, template.RuntimeTargetCode,
                template.MachineModelCode, template.ContentLengthBytes, template.ExportedAt, template.Description,
                template.MetadataJson, template.Id, contract.Id, contract.ContractChecksum);
            artifact.Id = artifactId;
            artifact.CreatedByAccountId = command.UserContext.AccountId;
            result.Add(definition.SourceKey, artifact);
            installation.AddMaterialization(ProductionPackageResourceKind.RobotArtifact,
                definition.SourceKey, artifact.Id.ToString("D"), artifact.Checksum);
        }
        return result;
    }

    private static ComposedPrograms ComposePrograms(
        InstallProductionPackageCommand command, ProductionPackageInstallation installation,
        ProductionPackageVersion version, IReadOnlyCollection<MaterializedProduct> products,
        IReadOnlyDictionary<string, RobotArtifact> artifacts,
        IReadOnlyCollection<RobotArtifactTechnicalContract> contracts,
        IReadOnlyDictionary<Guid, ProductOptionExecutionImpact> optionImpacts)
    {
        var contractById = contracts.ToDictionary(x => x.Id);
        var artifactDefinitions = version.Artifacts.ToDictionary(x => x.SourceKey, StringComparer.Ordinal);
        var programs = new Dictionary<string, RobotProgram>(StringComparer.Ordinal);
        var compositions = new List<ProductionComposition>();
        var selectedProductKeys = products.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        foreach (var route in version.Routes.Where(x => selectedProductKeys.Contains(x.ProductSourceKey))
                     .OrderBy(x => x.Priority).ThenBy(x => x.RouteCode))
        {
            var blueprint = version.Programs.Single(x => x.BlueprintCode == route.ProgramBlueprintCode);
            var product = products.Single(x => x.SourceKey == route.ProductSourceKey);
            var recipeKey = RecipeLookupKey(route.ProductVariantSourceKey, route.RecipeSourceKey);
            if (!product.RecipesByCode.TryGetValue(recipeKey, out var recipe) ||
                !product.RecipeRequirementsByCode.TryGetValue(recipeKey, out var recipeRequirements))
                throw new DomainRuleException($"Package route {route.RouteCode} references an unknown Recipe source key.");

            var orderedSlots = ProductionPackageDefinitionValidator.OrderSlots(blueprint, artifactDefinitions, contractById);
            var productDefinition = version.Products.Single(definition => definition.SourceKey == route.ProductSourceKey);
            var supportedOptionCodes = ProductionPackageDefinitionValidator.ResolveSupportedOptionCodes(
                route, productDefinition.ProductSnapshotJson, optionImpacts);
            var optionRequirements = supportedOptionCodes
                .SelectMany(code => product.OptionRequirementsByCode.TryGetValue(code, out var requirements)
                    ? requirements
                    : throw new DomainRuleException($"Package route {route.RouteCode} references an unknown supported option code."));
            ProductionPackageDefinitionValidator.ValidateEffects(orderedSlots, artifactDefinitions, contractById,
                recipeRequirements.Concat(optionRequirements)
                    .Select(x => new IngredientRequirement(x.IngredientCode, x.Quantity, x.Unit, x.OptionCode)).ToArray());
            var program = RobotProgram.CreateDraft($"PKG_{version.Version}_{route.RouteCode}",
                $"{blueprint.BlueprintCode} / {route.RouteCode}", TenantScopeResolver.Resolve(command.StoreId, command.KioskId),
                command.OrganizationId, command.StoreId, command.KioskId,
                description: $"Generated from package version {version.Version}, route {route.RouteCode}.");
            program.CreatedByAccountId = command.UserContext.AccountId;
            var order = 1;
            foreach (var slot in orderedSlots)
                program.AddArtifact(artifacts[slot.ArtifactSourceKey].Id, order++,
                    requiredOptionCode: ResolveRequiredOptionCode(
                        contractById[artifactDefinitions[slot.ArtifactSourceKey].TechnicalContractId]));
            programs.Add(route.RouteCode, program);
            installation.AddMaterialization(ProductionPackageResourceKind.RobotProgram,
                route.RouteCode, program.Id.ToString("D"));

            var input = JsonSerializer.Serialize(new { version.Id, version.ManifestChecksum, route.RouteCode,
                ProductVariantId = recipe.ProductVariantId, RecipeId = recipe.Id, blueprint.RuntimeTargetCode,
                blueprint.MachineModelCode, SupportedOptionCodes = supportedOptionCodes.Order(StringComparer.Ordinal),
                Slots = orderedSlots.Select(x => new { x.SlotCode, x.RequiredEffectCode,
                    ArtifactId = artifacts[x.ArtifactSourceKey].Id, artifacts[x.ArtifactSourceKey].Checksum }) });
            var report = JsonSerializer.Serialize(new { IsValid = true, RequiresUserAcknowledgement = true,
                Warnings = new[] { "Physical behavior has not been proven on the target kiosk." },
                OrderedEffects = orderedSlots.Select(x => x.RequiredEffectCode) });
            var composition = ProductionComposition.Create(installation.Id, command.OrganizationId,
                recipe.ProductVariantId, recipe.Id, null, blueprint.RuntimeTargetCode, blueprint.MachineModelCode,
                input, true, report);
            composition.Apply(program.Id);
            compositions.Add(composition);
        }
        return new ComposedPrograms(programs, compositions);
    }

    private static ConfigurationRelease CreateRelease(InstallProductionPackageCommand command,
        ProductionPackageInstallation installation, ProductionPackageVersion version, long releaseNumber,
        IReadOnlyCollection<MaterializedProduct> products, IReadOnlyDictionary<string, RobotProgram> programs,
        IReadOnlyDictionary<Guid, ProductOptionExecutionImpact> optionImpacts)
    {
        var release = ConfigurationRelease.CreateDraft(command.OrganizationId, releaseNumber);
        release.CreatedByAccountId = command.UserContext.AccountId;
        var selectedProductKeys = products.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        release.ReplaceRoutes(version.Routes.Where(x => selectedProductKeys.Contains(x.ProductSourceKey))
            .OrderBy(x => x.Priority).ThenBy(x => x.RouteCode).Select(routeDefinition =>
        {
            var product = products.Single(x => x.SourceKey == routeDefinition.ProductSourceKey);
            if (!product.RecipesByCode.TryGetValue(
                    RecipeLookupKey(routeDefinition.ProductVariantSourceKey, routeDefinition.RecipeSourceKey), out var recipe))
                throw new DomainRuleException("Package route Recipe source key was not materialized.");
            var capabilityCode = ProductionPackageDefinitionValidator.ValidateSingleCapability(
                routeDefinition.RequiredCapabilitiesJson);
            IReadOnlyCollection<(Guid, int, string)> bindings =
                [(programs[routeDefinition.RouteCode].Id, 1, capabilityCode)];
            var productDefinition = version.Products.Single(x => x.SourceKey == routeDefinition.ProductSourceKey);
            var supportedOptionCodes = ProductionPackageDefinitionValidator.ResolveSupportedOptionCodes(
                routeDefinition, productDefinition.ProductSnapshotJson, optionImpacts);
            return (recipe.ProductVariantId, recipe.Id, routeDefinition.RouteCode, routeDefinition.Priority,
                (string?)routeDefinition.RequiredCapabilitiesJson,
                (IReadOnlyCollection<string>)supportedOptionCodes.Order(StringComparer.Ordinal).ToArray(), bindings);
        }));
        return release;
    }

    private static string? ResolveRequiredOptionCode(RobotArtifactTechnicalContract contract)
    {
        var optionCodes = contract.Effects.Where(x => !string.IsNullOrWhiteSpace(x.OptionCode))
            .Select(x => x.OptionCode!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (optionCodes.Length > 1)
            throw new DomainRuleException("One artifact contract cannot be conditional on multiple product options in V1.");
        if (optionCodes.Length == 1 && contract.Effects.Any(x => string.IsNullOrWhiteSpace(x.OptionCode)))
            throw new DomainRuleException("An option-conditional artifact cannot also declare unconditional effects.");
        return optionCodes.SingleOrDefault()?.Trim().ToUpperInvariant();
    }

    private static string ComputeRequestChecksum(InstallProductionPackageCommand command,
        ProductionPackageVersion version, IReadOnlyCollection<string> selectedKeys)
    {
        var payload = JsonSerializer.Serialize(new
        {
            command.OrganizationId,
            command.StoreId,
            command.KioskId,
            PackageVersionId = version.Id,
            version.ManifestChecksum,
            ProductSourceKeys = selectedKeys.OrderBy(x => x, StringComparer.Ordinal)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private sealed record MaterializedProduct(string SourceKey, Product Product,
        IReadOnlyDictionary<string, Recipe> RecipesByCode,
        IReadOnlyDictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>> RecipeRequirementsByCode,
        IReadOnlyDictionary<string, IReadOnlyCollection<IngredientQuantityRequirement>> OptionRequirementsByCode);
    private sealed record IngredientQuantityRequirement(string IngredientCode, decimal Quantity, string Unit,
        string? OptionCode);
    private sealed record ComposedPrograms(IReadOnlyDictionary<string, RobotProgram> Programs,
        IReadOnlyCollection<ProductionComposition> Compositions);

    private static string RecipeLookupKey(string variantCode, string recipeCode) =>
        $"{variantCode.Trim().ToUpperInvariant()}::{recipeCode.Trim().ToUpperInvariant()}";
}
