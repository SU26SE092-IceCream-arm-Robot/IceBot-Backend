using Domain.Catalog.Entities;
using Domain.Common;
using Domain.ProductionPackages;
using Domain.SalesCatalog.Enums;

namespace Application.ProductionPackages.Upgrades;

public static class ProductionPackageUpgradePreparationPolicy
{
    public static void PrepareSuccessor(ProductionPackageUpgradePreparationState state,
        Guid actorId, DateTimeOffset now)
    {
        if (state.SourceInstallation.OwnershipMode != ProductionPackageOwnershipMode.PackageManaged ||
            state.TargetInstallation.OwnershipMode != ProductionPackageOwnershipMode.PackageManaged)
        {
            throw new DomainRuleException(
                "Package upgrade preparation requires package-managed source and successor installations.");
        }

        var sourceOptionKeys = state.SourceResources.Options.ToDictionary(item => item.Value.Id, item => item.Key);
        var targetOptionsByKey = state.TargetResources.Options;
        foreach (var (sourceKey, targetProduct) in state.TargetResources.Products)
        {
            var targetDefinition = state.TargetInstallation.PackageVersion.Products
                .Single(item => item.SourceKey == sourceKey);
            var targetCanonical = ProductionPackageProductSnapshotCodec
                .Deserialize(targetDefinition.ProductSnapshotJson).Product.Code;
            if (!state.SourceResources.Products.TryGetValue(sourceKey, out var sourceProduct))
            {
                state.Upgrade.AddCatalogIdentityChange(ProductionPackageUpgradeCatalogIdentityChange.Create(
                    sourceKey, null, targetProduct.Id,
                    null, null, targetProduct.Code,
                    targetCanonical, ProductionPackageUpgradeService.Hash(new { Source = targetCanonical, Target = targetProduct.Code }),
                    ProductionPackageUpgradeService.Hash(new { Source = ProductionPackageUpgradeService.HistoricalCode(targetCanonical, state.Upgrade.Id), Target = targetCanonical })));
                continue;
            }

            CopyProductCommercialState(sourceProduct, targetProduct, actorId, now);
            var historicalCode = ProductionPackageUpgradeService.HistoricalCode(sourceProduct.Code, state.Upgrade.Id);
            state.Upgrade.AddCatalogIdentityChange(ProductionPackageUpgradeCatalogIdentityChange.Create(
                sourceKey, sourceProduct.Id, targetProduct.Id, sourceProduct.Code, historicalCode,
                targetProduct.Code, targetCanonical,
                ProductionPackageUpgradeService.Hash(new { Source = sourceProduct.Code, Target = targetProduct.Code }),
                ProductionPackageUpgradeService.Hash(new { Source = historicalCode, Target = targetCanonical })));
            state.Upgrade.AddAvailabilityChange(ProductionPackageUpgradeAvailabilityChange.Create(
                ProductionPackageUpgradeAvailabilityResourceKind.Product, sourceKey,
                sourceProduct.Id, targetProduct.Id, sourceProduct.IsAvailable, targetProduct.IsAvailable,
                sourceProduct.IsAvailable));
        }

        foreach (var (sourceKey, targetVariant) in state.TargetResources.Variants)
        {
            if (!state.SourceResources.Variants.TryGetValue(sourceKey, out var sourceVariant)) continue;
            CopyVariantCommercialState(sourceVariant, targetVariant, actorId, now);
            state.Upgrade.AddAvailabilityChange(ProductionPackageUpgradeAvailabilityChange.Create(
                ProductionPackageUpgradeAvailabilityResourceKind.ProductVariant, sourceKey,
                sourceVariant.Id, targetVariant.Id, sourceVariant.IsAvailable, targetVariant.IsAvailable,
                sourceVariant.IsAvailable));
        }

        foreach (var (sourceKey, targetOption) in state.TargetResources.Options)
        {
            if (!state.SourceResources.Options.TryGetValue(sourceKey, out var sourceOption)) continue;
            targetOption.Name = sourceOption.Name;
            targetOption.Description = sourceOption.Description;
            targetOption.PriceDelta = sourceOption.PriceDelta;
            targetOption.DisplayOrder = sourceOption.DisplayOrder;
            targetOption.UpdatedAt = now;
            targetOption.UpdatedByAccountId = actorId;
            state.Upgrade.AddAvailabilityChange(ProductionPackageUpgradeAvailabilityChange.Create(
                ProductionPackageUpgradeAvailabilityResourceKind.ProductOption, sourceKey,
                sourceOption.Id, targetOption.Id, sourceOption.IsAvailable, targetOption.IsAvailable,
                sourceOption.IsAvailable));
        }

        foreach (var sourceGroup in state.SourceResources.Products.Values.SelectMany(product => product.OptionGroups))
        {
            var sourceProductKey = state.SourceResources.Products.Single(item => item.Value.Id == sourceGroup.ProductId).Key;
            if (!state.TargetResources.Products.TryGetValue(sourceProductKey, out var targetProduct)) continue;
            var targetGroup = targetProduct.OptionGroups.SingleOrDefault(group => group.Code == sourceGroup.Code);
            if (targetGroup is null) continue;
            targetGroup.Name = sourceGroup.Name;
            targetGroup.Description = sourceGroup.Description;
            targetGroup.DisplayOrder = sourceGroup.DisplayOrder;
            var sourceDefault = sourceGroup.ProductOptions.SingleOrDefault(option => option.IsDefault);
            foreach (var option in targetGroup.ProductOptions) option.IsDefault = false;
            if (sourceDefault is not null && sourceOptionKeys.TryGetValue(sourceDefault.Id, out var defaultKey) &&
                targetOptionsByKey.TryGetValue(defaultKey, out var targetDefault) &&
                targetDefault.OptionGroupId == targetGroup.Id)
                targetDefault.IsDefault = true;
        }

        AddMenuEvidence(state);
        foreach (var endpoint in state.EndpointTargets)
            state.Upgrade.AddEndpointTarget(ProductionPackageUpgradeEndpointTarget.Create(
                endpoint.Endpoint.Id, endpoint.Endpoint.KioskId, endpoint.ActiveReleaseId,
                endpoint.ActiveDeploymentId));
        state.Upgrade.MarkReadyForReview(state.TargetInstallation.Id, now);
    }

    private static void AddMenuEvidence(ProductionPackageUpgradePreparationState state)
    {
        var sourceProductById = state.SourceResources.Products.ToDictionary(item => item.Value.Id, item => item.Key);
        var sourceVariantById = state.SourceResources.Variants.ToDictionary(item => item.Value.Id, item => item.Key);
        var sourceRecipeById = state.SourceResources.Recipes.ToDictionary(item => item.Value.Id, item => item.Key);
        var sourceOptionById = state.SourceResources.Options.ToDictionary(item => item.Value.Id, item => item.Key);
        foreach (var item in state.MenuItems)
        {
            if (!sourceProductById.TryGetValue(item.ProductId, out var productKey) ||
                !sourceVariantById.TryGetValue(item.ProductVariantId, out var variantKey)) continue;
            state.TargetResources.Products.TryGetValue(productKey, out var targetProduct);
            state.TargetResources.Variants.TryGetValue(variantKey, out var targetVariant);
            var continuing = targetProduct is not null && targetVariant is not null;
            Guid? targetRecipeId = null;
            if (continuing && item.RecipeId.HasValue && sourceRecipeById.TryGetValue(item.RecipeId.Value, out var recipeKey) &&
                state.TargetResources.Recipes.TryGetValue(recipeKey, out var targetRecipe))
                targetRecipeId = targetRecipe.Id;
            var optionChanges = item.ProductOptions.Select(link =>
            {
                var key = sourceOptionById[link.ProductOptionId];
                return (key, (Guid?)link.ProductOptionId,
                    continuing && state.TargetResources.Options.TryGetValue(key, out var option)
                        ? (Guid?)option.Id : null);
            }).ToArray();
            var afterStatus = continuing ? item.Status : MenuItemStatus.Unavailable;
            var beforeChecksum = ProductionPackageUpgradeService.MenuBindingChecksum(item.ProductId, item.ProductVariantId, item.RecipeId,
                item.Status, optionChanges.Select(option => option.Item2));
            var afterChecksum = ProductionPackageUpgradeService.MenuBindingChecksum(targetProduct?.Id, targetVariant?.Id, targetRecipeId,
                afterStatus, optionChanges.Select(option => option.Item3));
            state.Upgrade.AddMenuChange(ProductionPackageUpgradeMenuChange.Create(
                continuing ? ProductionPackageUpgradeMenuChangeKind.Rebind :
                    ProductionPackageUpgradeMenuChangeKind.DeactivateRemoved,
                item.MenuId, item.Id, item.ProductId, targetProduct?.Id, item.ProductVariantId,
                targetVariant?.Id, item.RecipeId, targetRecipeId, item.Status, afterStatus,
                beforeChecksum, afterChecksum, optionChanges));
        }
    }

    private static void CopyProductCommercialState(Product source, Product target, Guid actorId, DateTimeOffset now)
    {
        target.Name = source.Name;
        target.DisplayName = source.DisplayName;
        target.Description = source.Description;
        target.BasePrice = source.BasePrice;
        target.Currency = source.Currency;
        target.ImageUrl = source.ImageUrl;
        target.CategoryId = source.CategoryId;
        target.UpdatedAt = now;
        target.UpdatedByAccountId = actorId;
    }

    private static void CopyVariantCommercialState(ProductVariant source, ProductVariant target,
        Guid actorId, DateTimeOffset now)
    {
        target.Name = source.Name;
        target.DisplayName = source.DisplayName;
        target.Description = source.Description;
        target.BasePrice = source.BasePrice;
        target.Currency = source.Currency;
        target.ImageUrl = source.ImageUrl;
        target.DisplayOrder = source.DisplayOrder;
        target.UpdatedAt = now;
        target.UpdatedByAccountId = actorId;
    }


}
