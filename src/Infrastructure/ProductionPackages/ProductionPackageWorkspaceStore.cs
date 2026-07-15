using Application.ProductionPackages.Workspace;
using Application.Inventory.Abstractions;
using Application.ProductionConfiguration.Readiness.Services;
using Domain.Catalog.Enums;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.SalesCatalog.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProductionPackages;

public sealed class ProductionPackageWorkspaceStore(IceBotDbContext db, IInventoryReadinessEvaluator inventoryReadiness)
    : IProductionPackageWorkspaceStore
{
    public Task<ProductionPackageWorkspaceScope?> GetScopeAsync(Guid organizationId, Guid installationId,
        CancellationToken cancellationToken) => db.ProductionPackageInstallations.AsNoTracking()
        .Where(x => x.OrganizationId == organizationId && x.Id == installationId)
        .Select(x => new ProductionPackageWorkspaceScope(x.OrganizationId, x.StoreId, x.KioskId))
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<ProductionPackageWorkspaceResult?> GetAsync(Guid organizationId, Guid installationId,
        CancellationToken cancellationToken)
    {
        var installation = await db.ProductionPackageInstallations.AsNoTracking()
            .Include(x => x.PackageVersion).ThenInclude(x => x.ProductionPackage)
            .Include(x => x.Materializations)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == installationId, cancellationToken);
        if (installation is null) return null;

        var materializations = installation.Materializations
            .Where(x => Guid.TryParse(x.TargetKey, out _))
            .Select(x => new MaterializedId(x.ResourceKind, x.SourceKey, Guid.Parse(x.TargetKey)))
            .ToArray();
        var invalidMaterializationBlockers = installation.Materializations
            .Where(x => !Guid.TryParse(x.TargetKey, out _))
            .Select(x => new WorkspaceBlockerResult("MaterializedResourceMissing",
                $"Materialized {x.ResourceKind} '{x.SourceKey}' has an invalid target identity.",
                x.ResourceKind.ToString())).ToArray();

        var products = await db.Products.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId &&
                Ids(materializations, ProductionPackageResourceKind.Product).Contains(x.Id))
            .OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var variants = await db.ProductVariants.AsNoTracking().Include(x => x.Product)
            .Where(x => x.Product.OrganizationId == organizationId &&
                Ids(materializations, ProductionPackageResourceKind.ProductVariant).Contains(x.Id))
            .OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var options = await db.ProductOptions.AsNoTracking().Include(x => x.OptionGroup).ThenInclude(x => x.Product)
            .Where(x => x.OptionGroup.Product.OrganizationId == organizationId &&
                Ids(materializations, ProductionPackageResourceKind.ProductOption).Contains(x.Id))
            .OrderBy(x => x.OptionGroup.Code).ThenBy(x => x.Code).ToListAsync(cancellationToken);
        var recipes = await db.Recipes.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId &&
                Ids(materializations, ProductionPackageResourceKind.Recipe).Contains(x.Id))
            .OrderBy(x => x.Code).ThenBy(x => x.Version).ToListAsync(cancellationToken);
        var artifacts = await db.RobotArtifacts.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId &&
                Ids(materializations, ProductionPackageResourceKind.RobotArtifact).Contains(x.Id))
            .OrderBy(x => x.ArtifactCode).ToListAsync(cancellationToken);
        var programs = await db.RobotPrograms.AsNoTracking().Include(x => x.RobotProgramArtifacts)
            .Where(x => x.OrganizationId == organizationId &&
                Ids(materializations, ProductionPackageResourceKind.RobotProgram).Contains(x.Id))
            .OrderBy(x => x.Code).ToListAsync(cancellationToken);

        var contractIds = artifacts.Where(x => x.TechnicalContractId.HasValue)
            .Select(x => x.TechnicalContractId!.Value).Distinct().ToArray();
        var contracts = await db.RobotArtifactTechnicalContracts.AsNoTracking()
            .Where(x => contractIds.Contains(x.Id) && (x.OrganizationId == null || x.OrganizationId == organizationId))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var technicallyReadyArtifactIds = artifacts.Where(artifact => artifact.TechnicalContractId.HasValue &&
                !string.IsNullOrWhiteSpace(artifact.TechnicalContractChecksum) &&
                contracts.TryGetValue(artifact.TechnicalContractId.Value, out var contract) &&
                contract.ContractChecksum == artifact.TechnicalContractChecksum &&
                contract.Status is RobotArtifactContractStatus.Published or RobotArtifactContractStatus.Retired)
            .Select(x => x.Id).ToHashSet();

        var releaseId = installation.DraftConfigurationReleaseId;
        var release = releaseId.HasValue
            ? await db.ConfigurationReleases.AsNoTracking().Include(x => x.ExecutionRoutes)
                .ThenInclude(x => x.ProductVariant).ThenInclude(x => x.Product)
                .Include(x => x.ExecutionRoutes).ThenInclude(x => x.Recipe)
                .Include(x => x.ExecutionRoutes).ThenInclude(x => x.RobotBindings).ThenInclude(x => x.RobotProgram)
                .ThenInclude(x => x.RobotProgramArtifacts)
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == releaseId, cancellationToken)
            : null;

        var endpoints = installation.KioskId.HasValue
            ? await db.KioskExecutionEndpoints.AsNoTracking().Include(x => x.SupportedRobotTargets)
                .Where(x => x.KioskId == installation.KioskId && x.Status == KioskExecutionEndpointStatus.Active)
                .ToListAsync(cancellationToken)
            : [];
        var endpointIds = endpoints.Select(x => x.Id).ToArray();
        var readinessByEndpoint = await db.ExecutionEndpointReadinessProjections.AsNoTracking()
            .Include(x => x.Capabilities).Where(x => endpointIds.Contains(x.KioskExecutionEndpointId))
            .ToDictionaryAsync(x => x.KioskExecutionEndpointId, cancellationToken);
        var requiredCapabilityCodes = release?.ExecutionRoutes.SelectMany(x => x.RobotBindings)
            .Select(x => x.RequiredWorkcellCapabilityCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
        var artifactsById = artifacts.ToDictionary(x => x.Id);
        var releaseProgramIds = release?.ExecutionRoutes.SelectMany(x => x.RobotBindings)
            .Select(x => x.RobotProgramId).ToHashSet() ?? [];
        var releasePrograms = programs.Where(x => releaseProgramIds.Contains(x.Id)).ToArray();
        var readyEndpoint = endpoints.FirstOrDefault(endpoint => releasePrograms.Length > 0 && releasePrograms.All(program =>
            program.RobotProgramArtifacts.All(membership => artifactsById.TryGetValue(membership.RobotArtifactId, out var artifact) &&
                endpoint.SupportsRobotTarget(artifact.RuntimeTargetCode, artifact.MachineModelCode, program.DeviceId))) &&
            readinessByEndpoint.TryGetValue(endpoint.Id, out var readiness) &&
            readiness.Readiness == ExecutionReadinessState.Ready && readiness.Safety == ExecutionSafetyState.Safe &&
            requiredCapabilityCodes.All(code => readiness.Capabilities.Any(capability => capability.IsAvailable &&
                string.Equals(capability.CapabilityCode, code, StringComparison.OrdinalIgnoreCase))));
        var endpointReady = readyEndpoint is not null;
        var latestDeploymentStatus = await GetLatestDeploymentStatusAsync(installation.KioskId, releaseId,
            cancellationToken);
        var deploymentActive = string.Equals(latestDeploymentStatus, "Active", StringComparison.Ordinal);
        var inventoryReady = true;
        if (installation.KioskId.HasValue && release is not null)
        {
            var inventoryResult = await inventoryReadiness.EvaluateKioskAsync(installation.KioskId.Value,
                ProductionInventoryReadinessGuard.BuildRoutes(release.ExecutionRoutes), cancellationToken);
            inventoryReady = inventoryResult?.IsReady == true;
        }
        var now = DateTimeOffset.UtcNow;
        var menuVariantIds = variants.Count == 0
            ? new HashSet<Guid>()
            : (await db.MenuItems.AsNoTracking().Where(x =>
                    variants.Select(v => v.Id).Contains(x.ProductVariantId) &&
                    x.Status == MenuItemStatus.Active &&
                    (x.EffectiveFrom == null || x.EffectiveFrom <= now) &&
                    (x.EffectiveTo == null || x.EffectiveTo >= now) &&
                    x.Menu.OrganizationId == organizationId && x.Menu.Status == MenuStatus.Active &&
                    (x.Menu.EffectiveFrom == null || x.Menu.EffectiveFrom <= now) &&
                    (x.Menu.EffectiveTo == null || x.Menu.EffectiveTo >= now) &&
                    (x.Menu.StoreId == null || x.Menu.StoreId == installation.StoreId) &&
                    (x.Menu.KioskId == null || x.Menu.KioskId == installation.KioskId))
                .Select(x => x.ProductVariantId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        var missingBlockers = BuildMissingResourceBlockers(materializations, products.Select(x => x.Id),
            variants.Select(x => x.Id), options.Select(x => x.Id), recipes.Select(x => x.Id),
            artifacts.Select(x => x.Id), programs.Select(x => x.Id), release?.Id);
        var blockers = BuildBlockers(installation.Status, products, variants, options, recipes, artifacts,
            programs, release, endpointReady, deploymentActive, inventoryReady, menuVariantIds, technicallyReadyArtifactIds)
            .Concat(missingBlockers).Concat(invalidMaterializationBlockers).ToArray();
        var actions = BuildActions(products, variants, options, recipes, artifacts, programs, release,
            installation.Id, installation.Status, installation.KioskId, endpointReady, menuVariantIds, technicallyReadyArtifactIds,
            missingBlockers.Count > 0 || invalidMaterializationBlockers.Length > 0, readyEndpoint, inventoryReady);

        return new ProductionPackageWorkspaceResult(
            installation.Id, installation.OrganizationId, installation.StoreId, installation.KioskId,
            installation.Status.ToString(), installation.OwnershipMode.ToString(),
            installation.PackageVersion.ProductionPackageId,
            installation.PackageVersion.ProductionPackage.Code,
            installation.PackageVersion.ProductionPackage.Name,
            installation.PackageVersionId, installation.PackageVersion.Version,
            products.Select(x => new WorkspaceResourceResult(x.Id, SourceKey(materializations,
                ProductionPackageResourceKind.Product, x.Id), x.Code, x.Name,
                x.IsAvailable ? "Available" : "Unavailable")).ToArray(),
            variants.Select(x => new WorkspaceResourceResult(x.Id, SourceKey(materializations,
                ProductionPackageResourceKind.ProductVariant, x.Id), x.Code, x.Name,
                x.IsAvailable ? "Available" : "Unavailable")).ToArray(),
            options.Select(x => new WorkspaceOptionResult(x.Id, SourceKey(materializations,
                ProductionPackageResourceKind.ProductOption, x.Id), x.OptionGroup.Code, x.Code, x.Name,
                x.IsAvailable ? "Available" : "Unavailable", x.ExecutionImpact.ToString())).ToArray(),
            recipes.Select(x => new WorkspaceResourceResult(x.Id, SourceKey(materializations,
                ProductionPackageResourceKind.Recipe, x.Id), x.Code, x.Name, x.Status.ToString())).ToArray(),
            artifacts.Select(x => new WorkspaceArtifactResult(x.Id, SourceKey(materializations,
                ProductionPackageResourceKind.RobotArtifact, x.Id), x.ArtifactCode, x.ArtifactName,
                x.Status.ToString(), x.TechnicalContractId,
                technicallyReadyArtifactIds.Contains(x.Id))).ToArray(),
            programs.Select(x => new WorkspaceProgramResult(x.Id, SourceKey(materializations,
                    ProductionPackageResourceKind.RobotProgram, x.Id), x.Code, x.Name, x.Status.ToString(),
                x.RobotProgramArtifacts.OrderBy(a => a.RunOrder).Select(a =>
                    new WorkspaceProgramArtifactResult(a.RobotArtifactId, a.RunOrder, a.RequiredOptionCode)).ToArray())).ToArray(),
            release is null ? null : new WorkspaceReleaseResult(release.Id, release.ReleaseNumber,
                release.Status.ToString(), release.ExecutionRoutes.Count, release.ReleaseChecksum),
            BuildTechnicalReadiness(installation.KioskId, endpointReady, latestDeploymentStatus, blockers),
            BuildCommercialReadiness(blockers),
            actions.Where(x => x.Kind == WorkspaceActionKind.Required).Select(x => x.Action).ToArray(),
            actions.Where(x => x.Kind == WorkspaceActionKind.Optional).Select(x => x.Action).ToArray(),
            actions.Where(x => x.Kind == WorkspaceActionKind.Recovery).Select(x => x.Action).ToArray());
    }

    private async Task<string?> GetLatestDeploymentStatusAsync(Guid? kioskId, Guid? releaseId,
        CancellationToken cancellationToken)
    {
        if (!kioskId.HasValue || !releaseId.HasValue) return null;
        var full = await db.KioskConfigurationDeployments.AsNoTracking()
            .Where(x => x.KioskId == kioskId && x.ConfigurationReleaseId == releaseId)
            .OrderByDescending(x => x.RequestedAt).Select(x => new { x.Status, x.RequestedAt })
            .FirstOrDefaultAsync(cancellationToken);
        var low = await db.ControllerArtifactSetDeployments.AsNoTracking()
            .Where(x => x.KioskId == kioskId && x.SourceConfigurationReleaseId == releaseId)
            .OrderByDescending(x => x.RequestedAt).Select(x => new { x.Status, x.RequestedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (full is null) return low?.Status.ToString();
        if (low is null) return full.Status.ToString();
        return (full.RequestedAt >= low.RequestedAt ? full.Status.ToString() : low.Status.ToString());
    }

    private static IReadOnlyCollection<WorkspaceBlockerResult> BuildBlockers(
        ProductionPackageInstallationStatus installationStatus,
        IReadOnlyCollection<Domain.Catalog.Entities.Product> products,
        IReadOnlyCollection<Domain.Catalog.Entities.ProductVariant> variants,
        IReadOnlyCollection<Domain.Catalog.Entities.ProductOption> options,
        IReadOnlyCollection<Domain.Catalog.Entities.Recipe> recipes,
        IReadOnlyCollection<RobotArtifact> artifacts,
        IReadOnlyCollection<RobotProgram> programs,
        Domain.ProductionConfiguration.Entities.ConfigurationRelease? release,
        bool endpointReady,
        bool deploymentActive,
        bool inventoryReady,
        IReadOnlySet<Guid> menuVariantIds,
        IReadOnlySet<Guid> technicallyReadyArtifactIds)
    {
        var result = new List<WorkspaceBlockerResult>();
        if (installationStatus != ProductionPackageInstallationStatus.Installed)
            result.Add(new("InstallationNotComplete", "Package installation has not completed."));
        result.AddRange(artifacts.Where(x => x.Status != RobotArtifactStatus.Published)
            .Select(x => new WorkspaceBlockerResult("ArtifactNotPublished", "Robot artifact must be published.",
                "RobotArtifact", x.Id, WorkspaceReadinessImpact.Technical)));
        result.AddRange(artifacts.Where(x => !technicallyReadyArtifactIds.Contains(x.Id))
            .Select(x => new WorkspaceBlockerResult("TechnicalContractNotReady",
                "Robot artifact technical contract is missing or does not match its checksum.", "RobotArtifact", x.Id,
                WorkspaceReadinessImpact.Technical)));
        result.AddRange(recipes.Where(x => x.Status is not RecipeStatus.Published and not RecipeStatus.Active)
            .Select(x => new WorkspaceBlockerResult("RecipeNotPublished", "Recipe must be Published or Active.", "Recipe", x.Id)));
        result.AddRange(programs.Where(x => x.Status != RobotProgramStatus.Published)
            .Select(x => new WorkspaceBlockerResult("ProgramNotPublished", "Robot program must be published.",
                "RobotProgram", x.Id, WorkspaceReadinessImpact.Technical)));
        if (release is null) result.Add(new("ReleaseMissing", "Draft configuration release is missing.",
            Impact: WorkspaceReadinessImpact.Technical));
        else if (release.Status != ConfigurationReleaseStatus.Published)
            result.Add(new("ReleaseNotPublished", "Configuration release must be published.",
                "ConfigurationRelease", release.Id, WorkspaceReadinessImpact.Technical));
        result.AddRange(products.Where(x => !x.IsAvailable).Select(x =>
            new WorkspaceBlockerResult("ProductUnavailable", "Product is not available for sale.",
                "Product", x.Id, WorkspaceReadinessImpact.Commercial)));
        result.AddRange(variants.Where(x => !x.IsAvailable).Select(x =>
            new WorkspaceBlockerResult("VariantUnavailable", "Product variant is not available for sale.",
                "ProductVariant", x.Id, WorkspaceReadinessImpact.Commercial)));
        result.AddRange(variants.Where(x => !menuVariantIds.Contains(x.Id)).Select(x =>
            new WorkspaceBlockerResult("MenuAssignmentMissing", "Product variant is not assigned to a menu.",
                "ProductVariant", x.Id, WorkspaceReadinessImpact.Commercial)));
        result.AddRange(options.GroupBy(x => x.OptionGroupId).Where(group =>
        {
            var optionGroup = group.First().OptionGroup;
            return optionGroup.IsActive && optionGroup.IsRequired &&
                   group.Count(option => option.IsAvailable) < optionGroup.MinSelections;
        }).Select(group =>
        {
            var optionGroup = group.First().OptionGroup;
            return new WorkspaceBlockerResult("RequiredOptionGroupUnavailable",
                $"Required option group {optionGroup.Code} does not have enough available choices.", "OptionGroup",
                Impact: WorkspaceReadinessImpact.Commercial);
        }));
        result.AddRange(GetRouteOptionPolicyDeficits(variants, options, release).Select(deficit =>
            new WorkspaceBlockerResult("RequiredOptionGroupUnsupported",
                $"No release route for variant {deficit.Variant.Code} supports enough available options in required group {deficit.GroupCode}.",
                "ProductVariant", deficit.Variant.Id, WorkspaceReadinessImpact.Commercial)));
        if (release?.Status == ConfigurationReleaseStatus.Published && !endpointReady)
            result.Add(new("ExecutionEndpointNotReady", "Target kiosk has no active execution endpoint.",
                Impact: WorkspaceReadinessImpact.Technical));
        if (release?.Status == ConfigurationReleaseStatus.Published && endpointReady && !inventoryReady)
            result.Add(new("InventoryTopologyNotReady", "Target kiosk inventory topology does not satisfy the release.",
                Impact: WorkspaceReadinessImpact.Technical));
        if (release?.Status == ConfigurationReleaseStatus.Published && endpointReady && !deploymentActive)
            result.Add(new("ReleaseNotActive", "Configuration release is not active on the target kiosk.",
                "ConfigurationRelease", release.Id, WorkspaceReadinessImpact.Technical));
        return result;
    }

    private static IReadOnlyCollection<ClassifiedWorkspaceAction> BuildActions(
        IReadOnlyCollection<Domain.Catalog.Entities.Product> products,
        IReadOnlyCollection<Domain.Catalog.Entities.ProductVariant> variants,
        IReadOnlyCollection<Domain.Catalog.Entities.ProductOption> options,
        IReadOnlyCollection<Domain.Catalog.Entities.Recipe> recipes,
        IReadOnlyCollection<RobotArtifact> artifacts,
        IReadOnlyCollection<RobotProgram> programs,
        Domain.ProductionConfiguration.Entities.ConfigurationRelease? release,
        Guid installationId, ProductionPackageInstallationStatus installationStatus,
        Guid? kioskId, bool endpointReady, IReadOnlySet<Guid> menuVariantIds,
        IReadOnlySet<Guid> technicallyReadyArtifactIds, bool hasMissingResources,
        KioskExecutionEndpoint? readyEndpoint, bool inventoryReady)
    {
        var actions = new List<ClassifiedWorkspaceAction>();
        var variantsById = variants.ToDictionary(variant => variant.Id);
        if (installationStatus == ProductionPackageInstallationStatus.Failed)
            actions.Add(Action("RetryInstallation", "ProductionPackageInstallation", installationId,
                kind: WorkspaceActionKind.Recovery));
        actions.AddRange(artifacts.Where(x => x.Status == RobotArtifactStatus.Draft)
            .Select(x => Action("PublishArtifact", "RobotArtifact", x.Id)));
        actions.AddRange(artifacts.Where(x => x.Status is RobotArtifactStatus.Retired or RobotArtifactStatus.Disabled)
            .Select(x => Action("ReplaceArtifact", "RobotArtifact", x.Id, kind: WorkspaceActionKind.Recovery)));
        actions.AddRange(recipes.Where(x => x.Status == RecipeStatus.Draft)
            .Select(x => Action("PublishRecipe", "Recipe", x.Id, context: RecipeContext(x, variantsById))));
        actions.AddRange(recipes.Where(x => x.Status == RecipeStatus.Retired)
            .Select(x => Action("CreateRecipeVersion", "Recipe", x.Id, kind: WorkspaceActionKind.Recovery,
                context: RecipeContext(x, variantsById))));
        var unpublishedArtifactIds = artifacts.Where(x => x.Status != RobotArtifactStatus.Published).Select(x => x.Id).ToHashSet();
        actions.AddRange(programs.Where(x => x.Status == RobotProgramStatus.Draft).Select(program =>
        {
            var missingContract = program.RobotProgramArtifacts.Any(x => !technicallyReadyArtifactIds.Contains(x.RobotArtifactId));
            var blocked = program.RobotProgramArtifacts.Any(x => unpublishedArtifactIds.Contains(x.RobotArtifactId)) || missingContract;
            var codes = new List<string>();
            if (program.RobotProgramArtifacts.Any(x => unpublishedArtifactIds.Contains(x.RobotArtifactId))) codes.Add("ArtifactNotPublished");
            if (missingContract) codes.Add("TechnicalContractNotReady");
            return Action("PublishProgram", "RobotProgram", program.Id, blocked, codes);
        }));
        actions.AddRange(programs.Where(x => x.Status == RobotProgramStatus.Retired)
            .Select(x => Action("ReplaceProgram", "RobotProgram", x.Id, kind: WorkspaceActionKind.Recovery)));
        if (release?.Status == ConfigurationReleaseStatus.Draft)
        {
            var codes = new List<string>();
            if (artifacts.Any(x => x.Status != RobotArtifactStatus.Published)) codes.Add("ArtifactNotPublished");
            if (recipes.Any(x => x.Status is not RecipeStatus.Published and not RecipeStatus.Active)) codes.Add("RecipeNotPublished");
            if (programs.Any(x => x.Status != RobotProgramStatus.Published)) codes.Add("ProgramNotPublished");
            if (artifacts.Any(x => !technicallyReadyArtifactIds.Contains(x.Id))) codes.Add("TechnicalContractNotReady");
            if (hasMissingResources) codes.Add("MaterializedResourceMissing");
            actions.Add(Action("PublishRelease", "ConfigurationRelease", release.Id, codes.Count > 0, codes));
        }
        if (release?.Status == ConfigurationReleaseStatus.Retired)
            actions.Add(Action("CreateReplacementRelease", "ConfigurationRelease", release.Id,
                kind: WorkspaceActionKind.Recovery));
        actions.AddRange(products.Where(x => !x.IsAvailable).Select(x => Action("EnableProduct", "Product", x.Id,
            context: new WorkspaceActionContextResult(ProductId: x.Id))));
        actions.AddRange(variants.Where(x => !x.IsAvailable).Select(x => Action("EnableVariant", "ProductVariant", x.Id,
            context: new WorkspaceActionContextResult(ProductId: x.ProductId, ProductVariantId: x.Id))));
        actions.AddRange(options.Where(x => !x.IsAvailable).Select(option =>
            Action("EnableOption", "ProductOption", option.Id, kind: WorkspaceActionKind.Optional,
                context: new WorkspaceActionContextResult(ProductId: option.OptionGroup.ProductId,
                    OptionGroupId: option.OptionGroupId))));
        actions.AddRange(ProductionPackageWorkspaceRules.BuildRequiredOptionGroupActions(options.Select(option =>
                new WorkspaceOptionAvailabilityInput(option.Id, option.OptionGroup.ProductId,
                    option.OptionGroupId, option.OptionGroup.Code,
                    option.OptionGroup.IsActive, option.OptionGroup.IsRequired,
                    option.OptionGroup.MinSelections, option.IsAvailable)).ToArray())
            .Select(action => new ClassifiedWorkspaceAction(WorkspaceActionKind.Required, action)));
        actions.AddRange(variants.Where(x => !menuVariantIds.Contains(x.Id)).Select(x =>
            Action("AssignVariantToMenu", "ProductVariant", x.Id,
                context: new WorkspaceActionContextResult(ProductId: x.ProductId, ProductVariantId: x.Id))));
        if (release is not null)
        {
            actions.AddRange(GetRouteOptionPolicyDeficits(variants, options, release).Select(deficit =>
                Action("ReviewRouteOptionPolicy", "ConfigurationRelease", release.Id,
                    resourceKey: deficit.GroupCode,
                    context: new WorkspaceActionContextResult(ProductId: deficit.Variant.ProductId,
                        ProductVariantId: deficit.Variant.Id))));
        }
        if (release?.Status == ConfigurationReleaseStatus.Published && kioskId.HasValue)
        {
            var deployBlockers = new List<string>();
            if (!endpointReady) deployBlockers.Add("ExecutionEndpointNotReady");
            if (!inventoryReady) deployBlockers.Add("InventoryTopologyNotReady");
            if (hasMissingResources) deployBlockers.Add("MaterializedResourceMissing");
            actions.Add(Action("DeployRelease", "ConfigurationRelease", release.Id,
                deployBlockers.Count > 0, deployBlockers,
                context: readyEndpoint is null ? null : new WorkspaceActionContextResult(
                    KioskExecutionEndpointId: readyEndpoint.Id,
                    ExecutionProfile: readyEndpoint.ExecutionProfile.ToString(),
                    DeploymentSelections: release.ExecutionRoutes.SelectMany(route => route.RobotBindings.Select(binding =>
                        new WorkspaceDeploymentSelectionResult(route.Id, binding.RobotProgramId))).ToArray())));
        }
        return actions;
    }

    private static ClassifiedWorkspaceAction Action(string code, string type, Guid id, bool blocked = false,
        IReadOnlyCollection<string>? blockers = null, WorkspaceActionKind kind = WorkspaceActionKind.Required,
        WorkspaceActionContextResult? context = null, string? resourceKey = null) =>
        new(kind, new WorkspaceActionResult(code, type, id, blocked, blockers ?? [],
            ResourceKey: resourceKey, Context: context));

    private static WorkspaceActionContextResult RecipeContext(Domain.Catalog.Entities.Recipe recipe,
        IReadOnlyDictionary<Guid, Domain.Catalog.Entities.ProductVariant> variants)
    {
        var variant = variants[recipe.ProductVariantId];
        return new WorkspaceActionContextResult(variant.ProductId, variant.Id);
    }

    private static IReadOnlyCollection<RouteOptionPolicyDeficit> GetRouteOptionPolicyDeficits(
        IReadOnlyCollection<Domain.Catalog.Entities.ProductVariant> variants,
        IReadOnlyCollection<Domain.Catalog.Entities.ProductOption> options,
        Domain.ProductionConfiguration.Entities.ConfigurationRelease? release)
    {
        if (release is null) return [];
        var deficits = new List<RouteOptionPolicyDeficit>();
        foreach (var variant in variants)
        {
            var routes = release.ExecutionRoutes.Where(route => route.ProductVariantId == variant.Id).ToArray();
            if (routes.Length == 0) continue;
            foreach (var group in options.Where(option => option.OptionGroup.ProductId == variant.ProductId)
                         .GroupBy(option => option.OptionGroupId))
            {
                var definition = group.First().OptionGroup;
                if (!definition.IsActive || !definition.IsRequired) continue;
                var available = group.Where(option => option.IsAvailable).ToArray();
                var policySatisfied = routes.Any(route =>
                {
                    var supported = route.GetSupportedOptionCodes().ToHashSet(StringComparer.OrdinalIgnoreCase);
                    return available.Count(option =>
                        option.ExecutionImpact == ProductOptionExecutionImpact.CommercialOnly ||
                        supported.Contains(option.Code)) >= definition.MinSelections;
                });
                if (!policySatisfied) deficits.Add(new RouteOptionPolicyDeficit(variant, definition.Code));
            }
        }
        return deficits;
    }

    private static WorkspaceTechnicalReadinessResult BuildTechnicalReadiness(Guid? kioskId, bool endpointReady,
        string? latestDeploymentStatus, IReadOnlyCollection<WorkspaceBlockerResult> blockers)
    {
        var technical = blockers.Where(blocker =>
            (blocker.Impact & WorkspaceReadinessImpact.Technical) != 0).ToArray();
        return new WorkspaceTechnicalReadinessResult(technical.Length == 0, kioskId.HasValue, endpointReady,
            latestDeploymentStatus, technical);
    }

    private static WorkspaceCommercialReadinessResult BuildCommercialReadiness(
        IReadOnlyCollection<WorkspaceBlockerResult> blockers)
    {
        var commercial = blockers.Where(blocker =>
            (blocker.Impact & WorkspaceReadinessImpact.Commercial) != 0).ToArray();
        return new WorkspaceCommercialReadinessResult(commercial.Length == 0, commercial);
    }

    private static Guid[] Ids(IEnumerable<MaterializedId> rows, ProductionPackageResourceKind kind) =>
        rows.Where(x => x.Kind == kind).Select(x => x.Id).ToArray();

    private static string SourceKey(IEnumerable<MaterializedId> rows, ProductionPackageResourceKind kind, Guid id) =>
        rows.First(x => x.Kind == kind && x.Id == id).SourceKey;

    private static IReadOnlyCollection<WorkspaceBlockerResult> BuildMissingResourceBlockers(
        IReadOnlyCollection<MaterializedId> rows,
        IEnumerable<Guid> productIds, IEnumerable<Guid> variantIds, IEnumerable<Guid> optionIds,
        IEnumerable<Guid> recipeIds, IEnumerable<Guid> artifactIds, IEnumerable<Guid> programIds, Guid? releaseId)
    {
        var loaded = new Dictionary<ProductionPackageResourceKind, HashSet<Guid>>
        {
            [ProductionPackageResourceKind.Product] = productIds.ToHashSet(),
            [ProductionPackageResourceKind.ProductVariant] = variantIds.ToHashSet(),
            [ProductionPackageResourceKind.ProductOption] = optionIds.ToHashSet(),
            [ProductionPackageResourceKind.Recipe] = recipeIds.ToHashSet(),
            [ProductionPackageResourceKind.RobotArtifact] = artifactIds.ToHashSet(),
            [ProductionPackageResourceKind.RobotProgram] = programIds.ToHashSet(),
            [ProductionPackageResourceKind.ConfigurationRelease] = releaseId.HasValue ? [releaseId.Value] : []
        };
        return rows.Where(x => loaded.TryGetValue(x.Kind, out var ids) && !ids.Contains(x.Id))
            .Select(x => new WorkspaceBlockerResult("MaterializedResourceMissing",
                $"Materialized {x.Kind} '{x.SourceKey}' is missing or outside the installation tenant scope.",
                x.Kind.ToString(), x.Id)).ToArray();
    }

    private sealed record MaterializedId(ProductionPackageResourceKind Kind, string SourceKey, Guid Id);
    private sealed record RouteOptionPolicyDeficit(
        Domain.Catalog.Entities.ProductVariant Variant,
        string GroupCode);
    private sealed record ClassifiedWorkspaceAction(WorkspaceActionKind Kind, WorkspaceActionResult Action);
    private enum WorkspaceActionKind { Required, Optional, Recovery }
}
