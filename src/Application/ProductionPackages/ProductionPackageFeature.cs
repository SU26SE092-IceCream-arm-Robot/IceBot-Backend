using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Artifacts;
using Domain.ProductionPackages;
using Domain.Tenants.Enums;

namespace Application.ProductionPackages;

public interface IProductionPackageStore
{
    Task<ProductionPackage?> GetPackageAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<ProductionPackage?> GetPackageWithVersionsAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionPackage>> ListPackagesAsync(CancellationToken cancellationToken);
    Task<ProductionPackageVersion?> GetVersionAsync(Guid packageId, Guid versionId, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionPackageVersion>> ListPublishedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> LoadGlobalProductsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifactTemplate>> LoadArtifactTemplatesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifactTechnicalContract>> LoadTechnicalContractsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task AddPackageAsync(ProductionPackage package, CancellationToken cancellationToken);
    Task<ProductionPackageVersion> CreateNextVersionAsync(Guid packageId, Guid? actorId,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record PackageProductSourceRequest(string SourceKey, Guid ProductId);
public sealed record PackageArtifactSourceRequest(string SourceKey, Guid RobotArtifactTemplateId);
public sealed record PackageProgramSlotRequest(string SlotCode, string ArtifactSourceKey, string RequiredEffectCode,
    string Phase, bool IsRequired, bool AllowMultiple, int SortHint);
public sealed record PackageProgramBlueprintRequest(string BlueprintCode, string RuntimeTargetCode,
    string MachineModelCode, IReadOnlyCollection<PackageProgramSlotRequest> Slots);
public sealed record PackageRouteBlueprintRequest(string RouteCode, string ProductSourceKey,
    string ProductVariantSourceKey, string RecipeSourceKey, IReadOnlyCollection<string> SupportedOptionCodes,
    string ProgramBlueprintCode, string RequiredCapabilitiesJson, int Priority);

public sealed class ReplaceProductionPackageDefinitionRequest
{
    public IReadOnlyCollection<PackageProductSourceRequest> Products { get; init; } = [];
    public IReadOnlyCollection<PackageArtifactSourceRequest> Artifacts { get; init; } = [];
    public IReadOnlyCollection<PackageProgramBlueprintRequest> Programs { get; init; } = [];
    public IReadOnlyCollection<PackageRouteBlueprintRequest> Routes { get; init; } = [];
}

public sealed record ProductionPackageProductChoiceResult(string SourceKey, string Code, string Name,
    IReadOnlyCollection<string> VariantCodes);
public sealed record ProductionPackageVersionResult(Guid Id, int Version, string Status, string? ManifestChecksum,
    IReadOnlyCollection<ProductionPackageProductChoiceResult> Products);
public sealed record ProductionPackageResult(Guid Id, string Code, string Name, string? Description, string Status,
    IReadOnlyCollection<ProductionPackageVersionResult> Versions);
public sealed record ProductionPackageDefinitionResult(Guid PackageId, Guid VersionId, int Version, string Status,
    IReadOnlyCollection<PackageProductSourceRequest> Products,
    IReadOnlyCollection<ProductionPackageArtifactDefinitionResult> Artifacts,
    IReadOnlyCollection<PackageProgramBlueprintRequest> Programs,
    IReadOnlyCollection<PackageRouteBlueprintRequest> Routes);
public sealed record ProductionPackageArtifactDefinitionResult(string SourceKey, Guid RobotArtifactTemplateId,
    string ArtifactChecksum, Guid TechnicalContractId, string TechnicalContractChecksum);

public sealed class ProductionPackageHandlers(IProductionPackageStore store)
{
    public async Task<ApiResult<ProductionPackageResult>> CreatePackageAsync(CurrentUserContext user, string code,
        string name, string? description, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageResult>.Fail("Access denied.", 403);
        try
        {
            var package = ProductionPackage.Create(code, name, description);
            package.CreatedByAccountId = user.AccountId;
            await store.AddPackageAsync(package, cancellationToken);
            return ApiResult<ProductionPackageResult>.Success(ToResult(package, []), "Production package created.", 201);
        }
        catch (Exception ex) when (ex is Domain.Common.DomainRuleException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return ApiResult<ProductionPackageResult>.Fail(ex is Domain.Common.DomainRuleException ? ex.Message : "Production package code already exists.", ex is Domain.Common.DomainRuleException ? 400 : 409);
        }
    }

    public async Task<ApiResult<ProductionPackageVersionResult>> CreateVersionAsync(CurrentUserContext user,
        Guid packageId, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageVersionResult>.Fail("Access denied.", 403);
        var package = await store.GetPackageAsync(packageId, false, cancellationToken);
        if (package is null || package.Status != ProductionPackageStatus.Active)
            return ApiResult<ProductionPackageVersionResult>.Fail("Active production package not found.", 404);
        var version = await store.CreateNextVersionAsync(packageId, user.AccountId, cancellationToken);
        return ApiResult<ProductionPackageVersionResult>.Success(ToResult(version), "Package version Draft created.", 201);
    }

    public async Task<ApiResult<ProductionPackageResult>> UpdatePackageAsync(CurrentUserContext user, Guid packageId,
        string name, string? description, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageResult>.Fail("Access denied.", 403);
        var package = await store.GetPackageWithVersionsAsync(packageId, true, cancellationToken);
        if (package is null) return ApiResult<ProductionPackageResult>.Fail("Production package not found.", 404);
        try
        {
            package.Update(name, description);
            package.UpdatedByAccountId = user.AccountId;
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<ProductionPackageResult>.Success(ToResult(package,
                package.Versions.OrderByDescending(x => x.Version).Select(ToResult).ToArray()));
        }
        catch (Domain.Common.DomainRuleException ex)
        {
            return ApiResult<ProductionPackageResult>.Fail(ex.Message, 409);
        }
    }

    public async Task<ApiResult<ProductionPackageResult>> RetirePackageAsync(CurrentUserContext user, Guid packageId,
        CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageResult>.Fail("Access denied.", 403);
        var package = await store.GetPackageWithVersionsAsync(packageId, true, cancellationToken);
        if (package is null) return ApiResult<ProductionPackageResult>.Fail("Production package not found.", 404);
        package.Retire();
        package.UpdatedByAccountId = user.AccountId;
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductionPackageResult>.Success(ToResult(package,
            package.Versions.OrderByDescending(x => x.Version).Select(ToResult).ToArray()));
    }

    public async Task<ApiResult<ProductionPackageDefinitionResult>> GetDefinitionAsync(CurrentUserContext user,
        Guid packageId, Guid versionId, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageDefinitionResult>.Fail("Access denied.", 403);
        var version = await store.GetVersionAsync(packageId, versionId, false, cancellationToken);
        if (version is null) return ApiResult<ProductionPackageDefinitionResult>.Fail("Package version not found.", 404);
        return ApiResult<ProductionPackageDefinitionResult>.Success(new ProductionPackageDefinitionResult(
            packageId, version.Id, version.Version, version.Status.ToString(),
            version.Products.OrderBy(x => x.SourceKey)
                .Select(x => new PackageProductSourceRequest(x.SourceKey, x.SourceProductId)).ToArray(),
            version.Artifacts.OrderBy(x => x.SourceKey).Select(x => new ProductionPackageArtifactDefinitionResult(
                x.SourceKey, x.RobotArtifactTemplateId, x.ArtifactChecksum, x.TechnicalContractId,
                x.TechnicalContractChecksum)).ToArray(),
            version.Programs.OrderBy(x => x.BlueprintCode).Select(x => new PackageProgramBlueprintRequest(
                x.BlueprintCode, x.RuntimeTargetCode, x.MachineModelCode,
                x.Slots.OrderBy(slot => slot.SortHint).ThenBy(slot => slot.SlotCode).Select(slot =>
                    new PackageProgramSlotRequest(slot.SlotCode, slot.ArtifactSourceKey, slot.RequiredEffectCode,
                        slot.Phase, slot.IsRequired, slot.AllowMultiple, slot.SortHint)).ToArray())).ToArray(),
            version.Routes.OrderBy(x => x.Priority).ThenBy(x => x.RouteCode).Select(x =>
                new PackageRouteBlueprintRequest(x.RouteCode, x.ProductSourceKey, x.ProductVariantSourceKey,
                    x.RecipeSourceKey, x.GetSupportedOptionCodes(), x.ProgramBlueprintCode,
                    x.RequiredCapabilitiesJson, x.Priority)).ToArray()));
    }

    public async Task<ApiResult<ProductionPackageVersionResult>> ReplaceDefinitionAsync(CurrentUserContext user,
        Guid packageId, Guid versionId, ReplaceProductionPackageDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageVersionResult>.Fail("Access denied.", 403);
        var version = await store.GetVersionAsync(packageId, versionId, true, cancellationToken);
        if (version is null) return ApiResult<ProductionPackageVersionResult>.Fail("Package version not found.", 404);

        var products = await store.LoadGlobalProductsAsync(request.Products.Select(x => x.ProductId).Distinct().ToArray(), cancellationToken);
        var templates = await store.LoadArtifactTemplatesAsync(request.Artifacts.Select(x => x.RobotArtifactTemplateId).Distinct().ToArray(), cancellationToken);
        var contractIds = templates.Where(x => x.TechnicalContractId.HasValue).Select(x => x.TechnicalContractId!.Value).Distinct().ToArray();
        var contracts = await store.LoadTechnicalContractsAsync(contractIds, cancellationToken);
        if (products.Count != request.Products.Select(x => x.ProductId).Distinct().Count() ||
            templates.Count != request.Artifacts.Select(x => x.RobotArtifactTemplateId).Distinct().Count())
            return ApiResult<ProductionPackageVersionResult>.Fail("One or more package sources were not found.", 400);
        if (products.Any(x => x.ScopeType != TenantScopeType.Global || x.OrganizationId.HasValue))
            return ApiResult<ProductionPackageVersionResult>.Fail("Production packages require global Product templates.", 400);
        if (templates.Any(x => x.Status != RobotArtifactStatus.Published || !x.TechnicalContractId.HasValue || string.IsNullOrWhiteSpace(x.TechnicalContractChecksum)))
            return ApiResult<ProductionPackageVersionResult>.Fail("Package artifacts require Published templates with technical contracts.", 400);
        var contractsById = contracts.ToDictionary(x => x.Id);
        if (templates.Any(x => !contractsById.TryGetValue(x.TechnicalContractId!.Value, out var contract) ||
            contract.Status != RobotArtifactContractStatus.Published || contract.ContractChecksum != x.TechnicalContractChecksum))
            return ApiResult<ProductionPackageVersionResult>.Fail("Artifact technical-contract publication does not match the template.", 409);

        try
        {
            var productsById = products.ToDictionary(x => x.Id);
            var templatesById = templates.ToDictionary(x => x.Id);
            version.ReplaceDefinition(
                request.Products.Select(x => ProductionPackageProductDefinition.Create(x.SourceKey, x.ProductId,
                    ProductionPackageProductSnapshotCodec.Serialize(productsById[x.ProductId]))).ToArray(),
                request.Artifacts.Select(x =>
                {
                    var template = templatesById[x.RobotArtifactTemplateId];
                    return ProductionPackageArtifactDefinition.Create(x.SourceKey, template.Id, template.Checksum,
                        template.TechnicalContractId!.Value, template.TechnicalContractChecksum!);
                }).ToArray(),
                request.Programs.Select(x => ProductionPackageProgramBlueprint.Create(x.BlueprintCode,
                    x.RuntimeTargetCode, x.MachineModelCode,
                    x.Slots.Select(s => (s.SlotCode, s.ArtifactSourceKey, s.RequiredEffectCode, s.Phase,
                        s.IsRequired, s.AllowMultiple, s.SortHint)))).ToArray(),
                request.Routes.Select(x => ProductionPackageRouteBlueprint.Create(x.RouteCode, x.ProductSourceKey,
                    x.ProductVariantSourceKey, x.RecipeSourceKey, x.SupportedOptionCodes,
                    x.ProgramBlueprintCode, x.RequiredCapabilitiesJson, x.Priority)).ToArray());
            ProductionPackageDefinitionValidator.Validate(version, contracts);
            version.UpdatedByAccountId = user.AccountId;
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<ProductionPackageVersionResult>.Success(ToResult(version), "Package definition replaced.");
        }
        catch (Domain.Common.DomainRuleException ex) { return ApiResult<ProductionPackageVersionResult>.Fail(ex.Message, 400); }
    }

    public async Task<ApiResult<ProductionPackageVersionResult>> PublishVersionAsync(CurrentUserContext user,
        Guid packageId, Guid versionId, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageVersionResult>.Fail("Access denied.", 403);
        var version = await store.GetVersionAsync(packageId, versionId, true, cancellationToken);
        if (version is null) return ApiResult<ProductionPackageVersionResult>.Fail("Package version not found.", 404);
        try
        {
            var contracts = await store.LoadTechnicalContractsAsync(
                version.Artifacts.Select(x => x.TechnicalContractId).Distinct().ToArray(), cancellationToken);
            ProductionPackageDefinitionValidator.Validate(version, contracts);
            version.Publish(DateTimeOffset.UtcNow, user.AccountId);
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<ProductionPackageVersionResult>.Success(ToResult(version), "Package version published.");
        }
        catch (Domain.Common.DomainRuleException ex) { return ApiResult<ProductionPackageVersionResult>.Fail(ex.Message, 400); }
    }

    public async Task<ApiResult<IReadOnlyCollection<ProductionPackageResult>>> ListManageAsync(
        CurrentUserContext user, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin)
            return ApiResult<IReadOnlyCollection<ProductionPackageResult>>.Fail("Access denied.", 403);
        var packages = await store.ListPackagesAsync(cancellationToken);
        return ApiResult<IReadOnlyCollection<ProductionPackageResult>>.Success(
            packages.Select(x => ToResult(x, x.Versions.OrderByDescending(v => v.Version).Select(ToResult).ToArray())).ToArray());
    }

    public async Task<ApiResult<ProductionPackageResult>> GetManageAsync(CurrentUserContext user,
        Guid packageId, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageResult>.Fail("Access denied.", 403);
        var package = await store.GetPackageWithVersionsAsync(packageId, false, cancellationToken);
        return package is null
            ? ApiResult<ProductionPackageResult>.Fail("Production package not found.", 404)
            : ApiResult<ProductionPackageResult>.Success(ToResult(package,
                package.Versions.OrderByDescending(x => x.Version).Select(ToResult).ToArray()));
    }

    public async Task<ApiResult<ProductionPackageVersionResult>> RetireVersionAsync(CurrentUserContext user,
        Guid packageId, Guid versionId, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<ProductionPackageVersionResult>.Fail("Access denied.", 403);
        var version = await store.GetVersionAsync(packageId, versionId, true, cancellationToken);
        if (version is null) return ApiResult<ProductionPackageVersionResult>.Fail("Package version not found.", 404);
        try
        {
            version.Retire(DateTimeOffset.UtcNow, user.AccountId);
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<ProductionPackageVersionResult>.Success(ToResult(version), "Package version retired.");
        }
        catch (Domain.Common.DomainRuleException ex)
        {
            return ApiResult<ProductionPackageVersionResult>.Fail(ex.Message, 409);
        }
    }

    public async Task<ApiResult<IReadOnlyCollection<ProductionPackageResult>>> ListCatalogAsync(CurrentUserContext user,
        Guid organizationId, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId, null, null))
            return ApiResult<IReadOnlyCollection<ProductionPackageResult>>.Fail("Access denied.", 403);
        var versions = await store.ListPublishedAsync(cancellationToken);
        var result = versions.GroupBy(x => x.ProductionPackage)
            .Select(x => ToResult(x.Key, x.Select(ToResult).ToArray())).ToArray();
        return ApiResult<IReadOnlyCollection<ProductionPackageResult>>.Success(result);
    }

    private static ProductionPackageVersionResult ToResult(ProductionPackageVersion version) =>
        new(version.Id, version.Version, version.Status.ToString(), version.ManifestChecksum,
            version.Products.OrderBy(x => x.SourceKey).Select(x =>
            {
                var product = ProductionPackageProductSnapshotCodec.Deserialize(x.ProductSnapshotJson).Product;
                return new ProductionPackageProductChoiceResult(x.SourceKey, product.Code,
                    product.DisplayName ?? product.Name,
                    product.Variants.OrderBy(variant => variant.DisplayOrder).Select(variant => variant.Code).ToArray());
            }).ToArray());
    private static ProductionPackageResult ToResult(ProductionPackage package, IReadOnlyCollection<ProductionPackageVersionResult> versions) =>
        new(package.Id, package.Code, package.Name, package.Description, package.Status.ToString(), versions);
}

internal static class ProductionPackageProductSnapshotCodec
{
    public static string Serialize(Product product)
    {
        var document = new
        {
            SchemaVersion = 2,
            Product = new
            {
                product.Id,
                product.Code,
                product.Name,
                product.DisplayName,
                product.Description,
                product.ProductType,
                product.BasePrice,
                product.Currency,
                product.PreparationTimeSeconds,
                product.ImageUrl,
                product.CategoryId,
                Variants = product.ProductVariants.Where(x => x.DeletedAt == null).OrderBy(x => x.DisplayOrder).Select(variant => new
                {
                    variant.Id,
                    variant.Code,
                    variant.Name,
                    variant.DisplayName,
                    variant.Description,
                    variant.VariantType,
                    variant.FulfillmentType,
                    variant.SizeCode,
                    variant.BasePrice,
                    variant.DisplayOrder,
                    variant.PreparationTimeSeconds,
                    variant.ImageUrl,
                    Recipes = variant.Recipes.Where(x => x.DeletedAt == null && x.Status != Domain.Catalog.Enums.RecipeStatus.Draft)
                        .OrderBy(x => x.Code).ThenByDescending(x => x.Version).Select(recipe => new
                        {
                            recipe.Id,
                            recipe.Code,
                            recipe.Name,
                            recipe.Version,
                            recipe.IsDefault,
                            recipe.YieldQuantity,
                            recipe.Unit,
                            recipe.EstimatedDurationSeconds,
                            recipe.EffectiveFrom,
                            recipe.EffectiveTo,
                            recipe.InstructionsSchemaVersion,
                            recipe.InstructionsJson,
                            Items = recipe.RecipeItems.Where(x => x.DeletedAt == null).OrderBy(x => x.StepOrder).Select(item => new
                            { item.Id, item.IngredientId, IngredientCode = item.Ingredient.Code, item.Quantity, item.Unit, item.StepOrder, item.IsOptional, item.Notes })
                        })
                }),
                OptionGroups = product.OptionGroups.OrderBy(x => x.DisplayOrder).Select(group => new
                {
                    group.Id,
                    group.Code,
                    group.Name,
                    group.Description,
                    group.SelectionType,
                    group.MinSelections,
                    group.MaxSelections,
                    group.IsRequired,
                    group.IsActive,
                    group.DisplayOrder,
                    Options = group.ProductOptions.Where(x => x.DeletedAt == null).OrderBy(x => x.DisplayOrder).Select(option => new
                    {
                        option.Id,
                        option.Code,
                        option.Name,
                        option.Description,
                        option.PriceDelta,
                        option.IsDefault,
                        option.ExecutionImpact,
                        option.DisplayOrder,
                        IngredientRequirements = option.IngredientRequirements.Where(x => x.DeletedAt == null)
                            .OrderBy(x => x.IngredientId).Select(requirement => new
                            { requirement.IngredientId, IngredientCode = requirement.Ingredient.Code, requirement.Quantity, requirement.Unit, requirement.RequiredWorkcellCapabilityCode })
                    })
                })
            }
        };
        return JsonSerializer.Serialize(document);
    }

    public static ProductionPackageProductSnapshotDocument Deserialize(string json)
    {
        try
        {
            var document = JsonSerializer.Deserialize<ProductionPackageProductSnapshotDocument>(json)
                ?? throw new Domain.Common.DomainRuleException("Package Product snapshot is empty.");
            if (document.SchemaVersion is not (1 or 2) || document.Product.Id == Guid.Empty)
                throw new Domain.Common.DomainRuleException("Package Product snapshot schema is unsupported.");
            if (document.SchemaVersion == 2 && document.Product.OptionGroups
                    .SelectMany(group => group.Options).Any(option => !option.ExecutionImpact.HasValue))
                throw new Domain.Common.DomainRuleException("Package Product snapshot V2 requires option execution impact.");
            return document;
        }
        catch (JsonException ex)
        {
            throw new Domain.Common.DomainRuleException($"Package Product snapshot is invalid: {ex.Message}");
        }
    }
}

internal sealed class ProductionPackageProductSnapshotDocument
{
    public int SchemaVersion { get; init; }
    public ProductionPackageProductSnapshot Product { get; init; } = new();
}

internal sealed class ProductionPackageProductSnapshot
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int? PreparationTimeSeconds { get; init; }
    public string? ImageUrl { get; init; }
    public long? CategoryId { get; init; }
    public IReadOnlyCollection<ProductionPackageVariantSnapshot> Variants { get; init; } = [];
    public IReadOnlyCollection<ProductionPackageOptionGroupSnapshot> OptionGroups { get; init; } = [];
}

internal sealed class ProductionPackageVariantSnapshot
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string VariantType { get; init; } = string.Empty;
    public Domain.Catalog.Enums.FulfillmentType FulfillmentType { get; init; }
    public string? SizeCode { get; init; }
    public decimal BasePrice { get; init; }
    public int DisplayOrder { get; init; }
    public int? PreparationTimeSeconds { get; init; }
    public string? ImageUrl { get; init; }
    public IReadOnlyCollection<ProductionPackageRecipeSnapshot> Recipes { get; init; } = [];
}

internal sealed class ProductionPackageRecipeSnapshot
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Version { get; init; }
    public bool IsDefault { get; init; }
    public decimal YieldQuantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int? EstimatedDurationSeconds { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public int InstructionsSchemaVersion { get; init; }
    public string? InstructionsJson { get; init; }
    public IReadOnlyCollection<ProductionPackageRecipeItemSnapshot> Items { get; init; } = [];
}

internal sealed class ProductionPackageRecipeItemSnapshot
{
    public Guid Id { get; init; }
    public Guid IngredientId { get; init; }
    public string IngredientCode { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int StepOrder { get; init; }
    public bool IsOptional { get; init; }
    public string? Notes { get; init; }
}

internal sealed class ProductionPackageOptionGroupSnapshot
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Domain.Catalog.Enums.OptionSelectionType SelectionType { get; init; }
    public int MinSelections { get; init; }
    public int MaxSelections { get; init; }
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public IReadOnlyCollection<ProductionPackageOptionSnapshot> Options { get; init; } = [];
}

internal sealed class ProductionPackageOptionSnapshot
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal PriceDelta { get; init; }
    public Domain.Catalog.Enums.ProductOptionExecutionImpact? ExecutionImpact { get; init; }
    public bool IsDefault { get; init; }
    public int DisplayOrder { get; init; }
    public IReadOnlyCollection<ProductionPackageOptionIngredientSnapshot> IngredientRequirements { get; init; } = [];
}

internal sealed class ProductionPackageOptionIngredientSnapshot
{
    public Guid IngredientId { get; init; }
    public string IngredientCode { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string RequiredWorkcellCapabilityCode { get; init; } = string.Empty;
}
