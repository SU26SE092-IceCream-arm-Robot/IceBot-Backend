using Application.Shared.Concurrency;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionPackages;

namespace Application.ProductionPackages.Upgrades;

public sealed class ProductionPackageUpgradeMutationPolicy(IProductionPackageUpgradeStore upgrades)
{
    public IReadOnlyCollection<TechnicalResourceMutationIdentity> MutationIdentities(
        ProductionPackageUpgrade upgrade) =>
        new[]
        {
            new TechnicalResourceMutationIdentity("ProductionPackageUpgrade", upgrade.Id.ToString("D")),
            TechnicalResourceMutationIdentity.PackageInstallation(upgrade.SourceInstallationId)
        }
        .Concat(upgrade.TargetInstallationId.HasValue
            ? [TechnicalResourceMutationIdentity.PackageInstallation(upgrade.TargetInstallationId.Value)]
            : Array.Empty<TechnicalResourceMutationIdentity>())
        .Concat(upgrade.CatalogIdentityChanges.SelectMany(item => new[]
        {
            new TechnicalResourceMutationIdentity("Product", item.SourceProductId?.ToString("D") ?? "new"),
            new TechnicalResourceMutationIdentity("Product", item.TargetProductId.ToString("D"))
        }))
        .Concat(upgrade.MenuChanges.Select(item =>
            new TechnicalResourceMutationIdentity("MenuItem", item.MenuItemId.ToString("D"))))
        .ToArray();

    public IReadOnlyCollection<TechnicalResourceMutationIdentity> PreparationMutationIdentities(
        ProductionPackageUpgrade upgrade,
        ProductionPackageUpgradeSourceState sourceState) =>
        MutationIdentities(upgrade)
            .Concat(sourceState.SourceResources.Products.Values.Select(product =>
                TechnicalResourceMutationIdentity.Product(product.Id)))
            .Concat(sourceState.MenuItems.Select(item =>
                TechnicalResourceMutationIdentity.Menu(item.MenuId)))
            .Concat(sourceState.EndpointTargets.Select(item =>
                TechnicalResourceMutationIdentity.ExecutionEndpoint(item.Endpoint.Id)))
            .Distinct()
            .ToArray();

    public void ValidateCutover(ProductionPackageUpgradeMutationState state)
    {
        if (state.Upgrade.Status != ProductionPackageUpgradeStatus.ReadyForReview ||
            state.SourceInstallation.Status != ProductionPackageInstallationStatus.Installed ||
            state.TargetInstallation.Status != ProductionPackageInstallationStatus.Installed)
            throw new DomainRuleException("Upgrade installations are not ready for cutover.");
        EnsurePackageManaged(state);
        if (state.TargetRelease.Status != ConfigurationReleaseStatus.Published)
            throw new DomainRuleException("Successor ConfigurationRelease must be Published before cutover.");

        foreach (var endpointTarget in state.Upgrade.EndpointTargets)
        {
            var endpoint = state.Endpoints.SingleOrDefault(item => item.Id == endpointTarget.KioskExecutionEndpointId)
                ?? throw new DomainRuleException("A snapshotted execution endpoint no longer exists.");
            var activeReleaseId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                ? endpoint.ActiveConfigurationReleaseId
                : endpoint.ActiveArtifactSetReleaseId;
            var activeDeploymentId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                ? endpoint.ActiveConfigurationDeploymentId
                : endpoint.ActiveArtifactSetDeploymentId;
            if (activeReleaseId != state.TargetRelease.Id || !activeDeploymentId.HasValue)
                throw new DomainRuleException("Successor release is not Active on every snapshotted execution endpoint.");
            EnsureActiveDeployment(
                state,
                endpointTarget,
                endpoint,
                activeDeploymentId.Value,
                state.TargetRelease.Id);
        }

        foreach (var identity in state.Upgrade.CatalogIdentityChanges)
        {
            var target = state.TargetResources.Products[identity.ProductSourceKey];
            Product? source = identity.SourceProductId.HasValue
                ? state.SourceResources.Products[identity.ProductSourceKey]
                : null;
            var checksum = ProductionPackageUpgradeService.Hash(new { Source = source?.Code ?? string.Empty, Target = target.Code });
            if (!string.Equals(checksum, identity.BeforeChecksum, StringComparison.Ordinal))
                throw new DomainRuleException("Catalog identity changed after upgrade preview.");
        }
        ValidateAvailability(state, after: false);
        ValidateMenuBindings(state, after: false);
    }

    public void ValidateRollback(ProductionPackageUpgradeMutationState state)
    {
        if (state.Upgrade.Status != ProductionPackageUpgradeStatus.RollbackPending ||
            state.SourceInstallation.Status != ProductionPackageInstallationStatus.Superseded ||
            state.TargetInstallation.Status != ProductionPackageInstallationStatus.Installed)
            throw new DomainRuleException("Upgrade installations are not ready for rollback.");
        EnsurePackageManaged(state);
        foreach (var identity in state.Upgrade.CatalogIdentityChanges)
        {
            var target = state.TargetResources.Products[identity.ProductSourceKey];
            Product? source = identity.SourceProductId.HasValue
                ? state.SourceResources.Products[identity.ProductSourceKey]
                : null;
            var checksum = ProductionPackageUpgradeService.Hash(new { Source = source?.Code ?? string.Empty, Target = target.Code });
            if (!string.Equals(checksum, identity.AfterChecksum, StringComparison.Ordinal))
                throw new DomainRuleException("Catalog identity changed after upgrade cutover.");
        }
        ValidateAvailability(state, after: true);
        ValidateMenuBindings(state, after: true);
    }

    public static void ValidateAvailability(ProductionPackageUpgradeMutationState state, bool after)
    {
        foreach (var change in state.Upgrade.AvailabilityChanges)
        {
            var (source, target) = ResolveAvailability(state, change);
            var expectedTarget = after ? change.TargetAvailabilityAfter : change.TargetAvailabilityBefore;
            if (source != change.SourceAvailabilityBefore || target != expectedTarget)
                throw new DomainRuleException("Catalog availability changed after upgrade preview or cutover.");
        }
    }

    public void ApplyAvailability(ProductionPackageUpgradeMutationState state, bool after)
    {
        foreach (var change in state.Upgrade.AvailabilityChanges)
        {
            var targetValue = after ? change.TargetAvailabilityAfter : change.TargetAvailabilityBefore;
            switch (change.ResourceKind)
            {
                case ProductionPackageUpgradeAvailabilityResourceKind.Product:
                    state.TargetResources.Products[change.ResourceSourceKey].IsAvailable = targetValue;
                    break;
                case ProductionPackageUpgradeAvailabilityResourceKind.ProductVariant:
                    state.TargetResources.Variants[change.ResourceSourceKey].IsAvailable = targetValue;
                    break;
                case ProductionPackageUpgradeAvailabilityResourceKind.ProductOption:
                    state.TargetResources.Options[change.ResourceSourceKey].IsAvailable = targetValue;
                    break;
                default:
                    throw new DomainRuleException("Unsupported upgrade availability resource kind.");
            }
        }
    }

    public static (bool Source, bool Target) ResolveAvailability(
        ProductionPackageUpgradeMutationState state, ProductionPackageUpgradeAvailabilityChange change) =>
        change.ResourceKind switch
        {
            ProductionPackageUpgradeAvailabilityResourceKind.Product =>
                (state.SourceResources.Products[change.ResourceSourceKey].IsAvailable,
                    state.TargetResources.Products[change.ResourceSourceKey].IsAvailable),
            ProductionPackageUpgradeAvailabilityResourceKind.ProductVariant =>
                (state.SourceResources.Variants[change.ResourceSourceKey].IsAvailable,
                    state.TargetResources.Variants[change.ResourceSourceKey].IsAvailable),
            ProductionPackageUpgradeAvailabilityResourceKind.ProductOption =>
                (state.SourceResources.Options[change.ResourceSourceKey].IsAvailable,
                    state.TargetResources.Options[change.ResourceSourceKey].IsAvailable),
            _ => throw new DomainRuleException("Unsupported upgrade availability resource kind.")
        };

    public static void ValidateMenuBindings(ProductionPackageUpgradeMutationState state, bool after)
    {
        foreach (var change in state.Upgrade.MenuChanges)
        {
            var item = state.MenuItems.SingleOrDefault(value => value.Id == change.MenuItemId)
                ?? throw new DomainRuleException("A snapshotted MenuItem no longer exists.");
            var actual = ProductionPackageUpgradeService.MenuBindingChecksum(item.ProductId, item.ProductVariantId, item.RecipeId,
                item.Status, item.ProductOptions.Select(option => (Guid?)option.ProductOptionId));
            var expected = after ? change.AfterBindingChecksum : change.BeforeBindingChecksum;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new DomainRuleException("MenuItem binding changed after upgrade preview or cutover.");
        }
    }

    public void ApplyMenuBindings(ProductionPackageUpgradeMutationState state, bool after)
    {
        foreach (var change in state.Upgrade.MenuChanges)
        {
            var item = state.MenuItems.Single(value => value.Id == change.MenuItemId);
            if (after)
            {
                if (change.ChangeKind == ProductionPackageUpgradeMenuChangeKind.Rebind)
                {
                    item.ProductId = change.AfterProductId!.Value;
                    item.ProductVariantId = change.AfterProductVariantId!.Value;
                    item.RecipeId = change.AfterRecipeId;
                }
                item.Status = change.AfterMenuItemStatus;
            }
            else
            {
                item.ProductId = change.BeforeProductId;
                item.ProductVariantId = change.BeforeProductVariantId;
                item.RecipeId = change.BeforeRecipeId;
                item.Status = change.BeforeMenuItemStatus;
            }
            var optionIds = change.OptionChanges
                .Select(option => after ? option.AfterProductOptionId : option.BeforeProductOptionId)
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
            upgrades.ReplaceMenuItemProductOptions(item, optionIds);
        }
    }

    public bool RollbackDeploymentsAreActive(ProductionPackageUpgradeMutationState state)
    {
        foreach (var target in state.Upgrade.EndpointTargets)
        {
            if (!target.RollbackDeploymentId.HasValue) return false;
            var endpoint = state.Endpoints.SingleOrDefault(item => item.Id == target.KioskExecutionEndpointId);
            if (endpoint is null) return false;
            var activeDeploymentId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                ? endpoint.ActiveConfigurationDeploymentId
                : endpoint.ActiveArtifactSetDeploymentId;
            var activeReleaseId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                ? endpoint.ActiveConfigurationReleaseId
                : endpoint.ActiveArtifactSetReleaseId;
            if (activeDeploymentId != target.RollbackDeploymentId ||
                activeReleaseId != target.SourceConfigurationReleaseId) return false;
            if (!ActiveDeploymentMatches(
                    state,
                    target,
                    endpoint,
                    target.RollbackDeploymentId.Value,
                    target.SourceConfigurationReleaseId)) return false;
        }
        return true;
    }

    private static void EnsurePackageManaged(ProductionPackageUpgradeMutationState state)
    {
        if (state.SourceInstallation.OwnershipMode != ProductionPackageOwnershipMode.PackageManaged ||
            state.TargetInstallation.OwnershipMode != ProductionPackageOwnershipMode.PackageManaged)
        {
            throw new DomainRuleException(
                "Package upgrade cutover and rollback require package-managed source and successor installations.");
        }
    }

    private static void EnsureActiveDeployment(
        ProductionPackageUpgradeMutationState state,
        ProductionPackageUpgradeEndpointTarget target,
        KioskExecutionEndpoint endpoint,
        Guid deploymentId,
        Guid releaseId)
    {
        if (!ActiveDeploymentMatches(state, target, endpoint, deploymentId, releaseId))
            throw new DomainRuleException(
                "Active deployment evidence does not match the snapshotted endpoint and release.");
    }

    private static bool ActiveDeploymentMatches(
        ProductionPackageUpgradeMutationState state,
        ProductionPackageUpgradeEndpointTarget target,
        KioskExecutionEndpoint endpoint,
        Guid deploymentId,
        Guid releaseId)
    {
        var expectedProfile = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? ConfigurationDeploymentProfile.FullEdge
            : ConfigurationDeploymentProfile.LowCostController;
        var deployment = state.ActiveDeployments.SingleOrDefault(item => item.DeploymentId == deploymentId);
        return deployment is not null &&
               deployment.Status == ConfigurationDeploymentReadStatus.Active &&
               deployment.Profile == expectedProfile &&
               deployment.OrganizationId == state.Upgrade.OrganizationId &&
               deployment.KioskId == target.KioskId &&
               deployment.KioskExecutionEndpointId == target.KioskExecutionEndpointId &&
               deployment.ConfigurationReleaseId == releaseId;
    }


}
