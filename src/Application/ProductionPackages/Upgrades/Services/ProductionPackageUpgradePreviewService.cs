using System.Globalization;
using Application.Identity.Tokens.Claims;
using Application.ProductionPackages.Installation;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.Common;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.Artifacts;
using Domain.SalesCatalog.Enums;

namespace Application.ProductionPackages.Upgrades;

public sealed class ProductionPackageUpgradePreviewService(
    IProductionPackageStore packages,
    IProductionPackageUpgradeStore upgrades)
{
    public async Task<ProductionPackageUpgradePreviewContext> BuildAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid sourceInstallationId,
        Guid targetVersionId,
        IReadOnlyCollection<string> requestedKeys,
        CancellationToken cancellationToken)
    {
        var sourceInstallation = await upgrades.GetSourceInstallationAsync(
            organizationId, sourceInstallationId, cancellationToken);
        if (sourceInstallation is null)
            return ProductionPackageUpgradePreviewContext.Fail("Source package installation not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId,
                sourceInstallation.StoreId, sourceInstallation.KioskId))
            return ProductionPackageUpgradePreviewContext.Fail("Access denied.", 403);

        ProductionPackageUpgradeSourceState? sourceState;
        try
        {
            sourceState = await upgrades.GetSourceStateAsync(
                organizationId, sourceInstallationId, false, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ProductionPackageUpgradePreviewContext.Fail(
                $"Source package materialization evidence is invalid: {ex.Message}", 409);
        }
        if (sourceState is null)
            return ProductionPackageUpgradePreviewContext.Fail("Source package installation not found.", 404);
        if (sourceState.SourceInstallation.Status != ProductionPackageInstallationStatus.Installed ||
            sourceState.SourceInstallation.OwnershipMode != ProductionPackageOwnershipMode.PackageManaged)
            return ProductionPackageUpgradePreviewContext.Fail(
                "Only an Installed PackageManaged installation can be upgraded.", 409);

        var sourceVersion = sourceState.SourceInstallation.PackageVersion;
        var targetVersion = await packages.GetVersionAsync(
            sourceVersion.ProductionPackageId, targetVersionId, false, cancellationToken);
        if (targetVersion is null || targetVersion.Status != ProductionPackageVersionStatus.Published ||
            string.IsNullOrWhiteSpace(targetVersion.ManifestChecksum))
            return ProductionPackageUpgradePreviewContext.Fail("Published target package version not found.", 404);
        if (targetVersion.Version <= sourceVersion.Version)
            return ProductionPackageUpgradePreviewContext.Fail(
                "Target package version must be newer than the installed version.", 409);

        IReadOnlySet<string> selectedKeys;
        try
        {
            var defaults = sourceState.SourceInstallation.GetSelectedProductSourceKeys()
                .Where(key => targetVersion.Products.Any(product => product.SourceKey == key)).ToArray();
            if (requestedKeys.Count == 0 && defaults.Length == 0)
                return ProductionPackageUpgradePreviewContext.Fail(
                    "Target package version has no Product matching the source installation; select successor Products explicitly.",
                    409);
            selectedKeys = ProductionPackageInstallationRequestRules.ResolveSelectedProductKeys(
                targetVersion, requestedKeys.Count == 0 ? defaults : requestedKeys);
            var contracts = await packages.LoadTechnicalContractsAsync(
                targetVersion.Artifacts.Select(item => item.TechnicalContractId).Distinct().ToArray(),
                cancellationToken);
            ProductionPackageDefinitionValidator.Validate(targetVersion, contracts);
        }
        catch (DomainRuleException ex)
        {
            return ProductionPackageUpgradePreviewContext.Fail(ex.Message, 409);
        }

        var sourceKeys = sourceState.SourceInstallation.GetSelectedProductSourceKeys()
            .ToHashSet(StringComparer.Ordinal);
        var targetDefinitions = targetVersion.Products.ToDictionary(item => item.SourceKey, StringComparer.Ordinal);
        var sourceDefinitions = sourceVersion.Products.ToDictionary(item => item.SourceKey, StringComparer.Ordinal);
        var added = selectedKeys.Except(sourceKeys).Order(StringComparer.Ordinal).ToArray();
        var removed = sourceKeys.Except(selectedKeys).Order(StringComparer.Ordinal).ToArray();
        var changed = selectedKeys.Intersect(sourceKeys)
            .Where(key => sourceDefinitions[key].ProductSnapshotChecksum != targetDefinitions[key].ProductSnapshotChecksum)
            .Order(StringComparer.Ordinal).ToArray();
        var blockers = ValidateManagedTechnicalState(sourceState, sourceDefinitions);
        blockers.AddRange(ValidateManagedArtifactState(sourceState, sourceVersion));
        blockers.AddRange(ValidateDefaultOptionContinuity(sourceState, targetDefinitions, selectedKeys));
        var canonicalCodes = selectedKeys.Select(key => ProductionPackageProductSnapshotCodec
            .Deserialize(targetDefinitions[key].ProductSnapshotJson).Product.Code).ToArray();
        var codeConflicts = await upgrades.ListConflictingProductCodesAsync(
            organizationId, sourceState.SourceInstallation.StoreId, sourceState.SourceInstallation.KioskId,
            canonicalCodes, sourceState.SourceResources.Products.Values.Select(product => product.Id).ToArray(),
            cancellationToken);
        blockers.AddRange(codeConflicts.Select(code => $"CanonicalProductCodeConflict:{code}"));
        if (sourceState.MenuItems.Any(item => item.Status == MenuItemStatus.Active) &&
            sourceState.EndpointTargets.Count == 0)
            blockers.Add("SourceDeploymentEvidenceMissing");

        var warnings = new List<string>();
        if (removed.Length > 0) warnings.Add("Removed package products will be made unavailable at cutover.");
        if (added.Length > 0) warnings.Add("New package products are not placed on a Menu automatically.");

        var products = sourceKeys.Union(selectedKeys).Order(StringComparer.Ordinal).Select(key =>
        {
            sourceState.SourceResources.Products.TryGetValue(key, out var current);
            var incoming = targetDefinitions.TryGetValue(key, out var definition)
                ? ProductionPackageProductSnapshotCodec.Deserialize(definition.ProductSnapshotJson).Product
                : null;
            var kind = added.Contains(key) ? "Added" : removed.Contains(key) ? "Removed" :
                changed.Contains(key) ? "Changed" : "Unchanged";
            return new ProductionPackageUpgradeProductPreview(
                key, kind, current?.Code, incoming?.Code, current?.PreparationTimeSeconds,
                incoming?.PreparationTimeSeconds, current is not null && incoming is not null,
                current?.IsAvailable ?? false);
        }).ToArray();
        var sourceProductKeysById = sourceState.SourceResources.Products
            .ToDictionary(item => item.Value.Id, item => item.Key);
        var menus = sourceState.MenuItems.Select(item =>
        {
            var continuing = sourceProductKeysById.TryGetValue(item.ProductId, out var key) && selectedKeys.Contains(key);
            return new ProductionPackageUpgradeMenuPreview(
                item.MenuId, item.Id, item.Code, continuing ? "Rebind" : "DeactivateRemoved",
                item.Status.ToString(), item.PreparationTimeSeconds);
        }).ToArray();
        var selection = ProductionPackageInstallationSelectionRules.Resolve(targetVersion, selectedKeys);
        var artifacts = selection.Artifacts.OrderBy(item => item.SourceKey).Select(item =>
        {
            var reusable = sourceState.SourceResources.Artifacts.TryGetValue(item.SourceKey, out var current) &&
                           current.Status != RobotArtifactStatus.Retired &&
                           current.SourceRobotArtifactTemplateId == item.RobotArtifactTemplateId &&
                           current.Checksum == item.ArtifactChecksum &&
                           current.TechnicalContractId == item.TechnicalContractId &&
                           current.TechnicalContractChecksum == item.TechnicalContractChecksum;
            return new ProductionPackageUpgradeArtifactPreview(
                item.SourceKey, item.ArtifactChecksum,
                reusable ? "ReuseExistingCandidate" : "MaterializeSuccessorCopy");
        }).ToArray();
        var endpoints = sourceState.EndpointTargets.Select(item => new ProductionPackageUpgradeEndpointPreview(
            item.Endpoint.Id, item.Endpoint.KioskId, item.ActiveReleaseId, item.ActiveDeploymentId)).ToArray();
        var result = new ProductionPackageUpgradePreviewResult(
            sourceInstallationId, sourceVersion.Id, targetVersion.Id,
            ComputePreviewChecksum(sourceState, targetVersion, selectedKeys),
            selectedKeys.Order(StringComparer.Ordinal).ToArray(), added, removed, changed,
            sourceState.MenuItems.Count, sourceState.EndpointTargets.Count,
            products, menus, artifacts, endpoints, blockers, warnings);
        return new ProductionPackageUpgradePreviewContext(
            ApiResult<ProductionPackageUpgradePreviewResult>.Success(result), sourceState, targetVersion);
    }

    private static List<string> ValidateManagedTechnicalState(
        ProductionPackageUpgradeSourceState state,
        IReadOnlyDictionary<string, ProductionPackageProductDefinition> definitions)
    {
        var blockers = new List<string>();
        foreach (var (sourceKey, product) in state.SourceResources.Products)
        {
            if (!definitions.TryGetValue(sourceKey, out var definition))
            {
                blockers.Add($"ManagedProductDefinitionMissing:{sourceKey}");
                continue;
            }
            var expected = ProductionPackageProductSnapshotCodec.Deserialize(definition.ProductSnapshotJson).Product;
            if (TechnicalProductChecksum(product) != TechnicalProductChecksum(expected))
                blockers.Add($"ManagedFieldDrift:{sourceKey}");
        }
        return blockers;
    }

    private static IReadOnlyCollection<string> ValidateDefaultOptionContinuity(
        ProductionPackageUpgradeSourceState state,
        IReadOnlyDictionary<string, ProductionPackageProductDefinition> targetDefinitions,
        IReadOnlyCollection<string> selectedKeys)
    {
        var blockers = new List<string>();
        foreach (var sourceKey in selectedKeys)
        {
            if (!state.SourceResources.Products.TryGetValue(sourceKey, out var sourceProduct)) continue;
            var targetProduct = ProductionPackageProductSnapshotCodec
                .Deserialize(targetDefinitions[sourceKey].ProductSnapshotJson).Product;
            foreach (var sourceGroup in sourceProduct.OptionGroups)
            {
                var sourceDefault = sourceGroup.ProductOptions.SingleOrDefault(option => option.IsDefault);
                if (sourceDefault is null) continue;
                var targetGroup = targetProduct.OptionGroups.SingleOrDefault(group => group.Code == sourceGroup.Code);
                if (targetGroup?.Options.Any(option => option.Code == sourceDefault.Code) != true &&
                    targetGroup is { IsRequired: true, MinSelections: > 0 })
                    blockers.Add($"DefaultOptionReplacementRequired:{sourceKey}:{sourceGroup.Code}");
            }
        }
        return blockers;
    }

    private static IReadOnlyCollection<string> ValidateManagedArtifactState(
        ProductionPackageUpgradeSourceState state,
        ProductionPackageVersion sourceVersion)
    {
        var definitions = sourceVersion.Artifacts.ToDictionary(item => item.SourceKey, StringComparer.Ordinal);
        return state.SourceResources.Artifacts.Where(item =>
                !definitions.TryGetValue(item.Key, out var definition) ||
                item.Value.ArtifactCode != definition.SourceKey ||
                item.Value.SourceRobotArtifactTemplateId != definition.RobotArtifactTemplateId ||
                item.Value.Checksum != definition.ArtifactChecksum ||
                item.Value.TechnicalContractId != definition.TechnicalContractId ||
                item.Value.TechnicalContractChecksum != definition.TechnicalContractChecksum ||
                item.Value.Status == RobotArtifactStatus.Retired)
            .Select(item => $"ManagedArtifactDrift:{item.Key}").ToArray();
    }

    private static string ComputePreviewChecksum(
        ProductionPackageUpgradeSourceState source,
        ProductionPackageVersion target,
        IReadOnlyCollection<string> selectedKeys) => ProductionPackageUpgradeService.Hash(new
        {
            source.SourceInstallation.Id,
            source.SourceInstallation.PackageManifestChecksum,
            TargetManifestChecksum = target.ManifestChecksum,
            Selected = selectedKeys.Order(StringComparer.Ordinal),
            Commercial = source.SourceResources.Products.OrderBy(item => item.Key).Select(item => new
            {
                item.Key,
                item.Value.Name,
                item.Value.DisplayName,
                item.Value.Description,
                item.Value.BasePrice,
                item.Value.Currency,
                item.Value.ImageAssetId,
                item.Value.ImageAltText,
                item.Value.CategoryId,
                item.Value.IsAvailable,
                Variants = item.Value.ProductVariants.OrderBy(value => value.Code).Select(value => new
                {
                    value.Code,
                    value.Name,
                    value.DisplayName,
                    value.Description,
                    value.BasePrice,
                    value.Currency,
                    value.ImageAssetId,
                    value.ImageAltText,
                    value.DisplayOrder,
                    value.IsAvailable
                }),
                Options = item.Value.OptionGroups.SelectMany(group => group.ProductOptions)
                    .OrderBy(value => value.Code).Select(value => new
                    { value.Code, value.Name, value.Description, value.PriceDelta, value.DisplayOrder, value.IsAvailable, value.IsDefault })
            }),
            Menus = source.MenuItems.OrderBy(item => item.Id).Select(item => new
            {
                item.Id,
                item.ProductId,
                item.ProductVariantId,
                item.RecipeId,
                item.Status,
                Options = item.ProductOptions.Select(option => option.ProductOptionId).Order()
            }),
            Endpoints = source.EndpointTargets.OrderBy(item => item.Endpoint.Id).Select(item => new
            { item.Endpoint.Id, item.ActiveReleaseId, item.ActiveDeploymentId })
        });

    private static string TechnicalProductChecksum(Product product) => ProductionPackageUpgradeService.Hash(new
    {
        product.Code,
        product.ProductType,
        product.PreparationTimeSeconds,
        Variants = product.ProductVariants.OrderBy(item => item.Code).Select(item => new
        {
            item.Code,
            item.VariantType,
            item.FulfillmentType,
            item.SizeCode,
            item.PreparationTimeSeconds,
            Recipes = item.Recipes.OrderBy(recipe => recipe.Code).Select(recipe => new
            {
                recipe.Code,
                recipe.IsDefault,
                YieldQuantity = CanonicalDecimal(recipe.YieldQuantity),
                recipe.Unit,
                recipe.EstimatedDurationSeconds,
                recipe.EffectiveFrom,
                recipe.EffectiveTo,
                recipe.InstructionsSchemaVersion,
                recipe.InstructionsJson,
                Items = recipe.RecipeItems.OrderBy(value => value.StepOrder).Select(value => new
                { value.IngredientId, Quantity = CanonicalDecimal(value.Quantity), value.Unit, value.StepOrder, value.IsOptional, value.Notes })
            })
        }),
        Groups = product.OptionGroups.OrderBy(item => item.Code).Select(item => new
        {
            item.Code,
            item.SelectionType,
            item.MinSelections,
            item.MaxSelections,
            item.IsRequired,
            item.IsActive,
            Options = item.ProductOptions.OrderBy(option => option.Code).Select(option => new
            {
                option.Code,
                option.ExecutionImpact,
                Requirements = option.IngredientRequirements.OrderBy(value => value.IngredientId).Select(value => new
                { value.IngredientId, Quantity = CanonicalDecimal(value.Quantity), value.Unit, value.RequiredWorkcellCapabilityCode })
            })
        })
    });

    private static string TechnicalProductChecksum(ProductionPackageProductSnapshot product) =>
        ProductionPackageUpgradeService.Hash(new
        {
            product.Code,
            product.ProductType,
            product.PreparationTimeSeconds,
            Variants = product.Variants.OrderBy(item => item.Code).Select(item => new
            {
                item.Code,
                item.VariantType,
                item.FulfillmentType,
                item.SizeCode,
                item.PreparationTimeSeconds,
                Recipes = item.Recipes.OrderBy(recipe => recipe.Code).Select(recipe => new
                {
                    recipe.Code,
                    recipe.IsDefault,
                    YieldQuantity = CanonicalDecimal(recipe.YieldQuantity),
                    recipe.Unit,
                    recipe.EstimatedDurationSeconds,
                    recipe.EffectiveFrom,
                    recipe.EffectiveTo,
                    recipe.InstructionsSchemaVersion,
                    recipe.InstructionsJson,
                    Items = recipe.Items.OrderBy(value => value.StepOrder).Select(value => new
                    { value.IngredientId, Quantity = CanonicalDecimal(value.Quantity), value.Unit, value.StepOrder, value.IsOptional, value.Notes })
                })
            }),
            Groups = product.OptionGroups.OrderBy(item => item.Code).Select(item => new
            {
                item.Code,
                item.SelectionType,
                item.MinSelections,
                item.MaxSelections,
                item.IsRequired,
                item.IsActive,
                Options = item.Options.OrderBy(option => option.Code).Select(option => new
                {
                    option.Code,
                    option.ExecutionImpact,
                    Requirements = option.IngredientRequirements.OrderBy(value => value.IngredientId).Select(value => new
                    { value.IngredientId, Quantity = CanonicalDecimal(value.Quantity), value.Unit, value.RequiredWorkcellCapabilityCode })
                })
            })
        });

    private static decimal CanonicalDecimal(decimal value) => decimal.Parse(
        value.ToString("G29", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
