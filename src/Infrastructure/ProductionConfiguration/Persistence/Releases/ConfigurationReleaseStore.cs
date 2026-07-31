using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.ReadModels;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.RobotConfiguration.Programs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.ProductionConfiguration.Persistence.Releases;

public sealed class ConfigurationReleaseStore : IConfigurationReleaseStore
{
    private readonly IceBotDbContext _dbContext;

    public ConfigurationReleaseStore(IceBotDbContext dbContext) => _dbContext = dbContext;

    public Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default) =>
        ReleaseGraph().FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);

    public Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default) =>
        ReleaseGraph().FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);

    public Task<ConfigurationRelease?> GetReleaseByIdAsync(Guid releaseId, CancellationToken cancellationToken = default) =>
        ReleaseGraph(asNoTracking: true).FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);

    public Task<ConfigurationRelease?> GetReleaseForEditAsync(Guid releaseId, CancellationToken cancellationToken = default) =>
        ReleaseGraph().FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);

    public Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        _dbContext.Organizations.AnyAsync(organization => organization.Id == organizationId && organization.DeletedAt == null, cancellationToken);

    public async Task<ConfigurationRelease> CreateNextReleaseAsync(
        Guid organizationId,
        Func<long, ConfigurationRelease> releaseFactory,
        CancellationToken cancellationToken = default)
    {
        var ownsTransaction = _dbContext.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({organizationId.ToString("D")}, 0));", cancellationToken);
        var maximum = await _dbContext.ConfigurationReleases.WhereNotDeleted()
            .Where(release => release.OrganizationId == organizationId)
            .Select(release => (long?)release.ReleaseNumber)
            .MaxAsync(cancellationToken);
        var release = releaseFactory((maximum ?? 0) + 1);
        await _dbContext.ConfigurationReleases.AddAsync(release, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return release;
    }

    public Task<int> CountReleasesAsync(Guid? organizationId, ConfigurationReleaseStatus? status, bool isSystemAdmin, IEnumerable<Guid> allowedOrganizationIds, CancellationToken cancellationToken = default) =>
        BuildReleaseListQuery(organizationId, status, isSystemAdmin, allowedOrganizationIds).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ConfigurationReleaseSummaryReadModel>> ListReleasesAsync(
        Guid? organizationId, ConfigurationReleaseStatus? status, bool isSystemAdmin, IEnumerable<Guid> allowedOrganizationIds,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        await BuildReleaseListQuery(organizationId, status, isSystemAdmin, allowedOrganizationIds)
            .OrderByDescending(release => release.ReleaseNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(release => new ConfigurationReleaseSummaryReadModel
            {
                Id = release.Id, OrganizationId = release.OrganizationId, ReleaseNumber = release.ReleaseNumber,
                Status = release.Status.ToString(), ReleaseChecksum = release.ReleaseChecksum,
                PublishedAt = release.PublishedAt, PublishedByAccountId = release.PublishedByAccountId,
                RouteCount = release.ExecutionRoutes.Count
            }).ToListAsync(cancellationToken);

    public async Task<ConfigurationReleaseAuthoringOptionsReadModel> GetAuthoringOptionsAsync(
        Guid organizationId, Guid? productVariantId, string? search, int limit, CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var variantQuery = _dbContext.ProductVariants.AsNoTracking().Where(variant =>
            variant.DeletedAt == null && variant.Product.DeletedAt == null &&
            variant.FulfillmentType == FulfillmentType.MachineProduced &&
            (!variant.Product.OrganizationId.HasValue || variant.Product.OrganizationId == organizationId));
        if (productVariantId.HasValue) variantQuery = variantQuery.Where(variant => variant.Id == productVariantId.Value);
        if (term is not null) variantQuery = variantQuery.Where(variant =>
            EF.Functions.ILike(variant.Code, $"%{term}%") || EF.Functions.ILike(variant.Name, $"%{term}%") ||
            EF.Functions.ILike(variant.Product.Code, $"%{term}%") || EF.Functions.ILike(variant.Product.Name, $"%{term}%"));
        var variants = await variantQuery.OrderBy(variant => variant.Product.Name).ThenBy(variant => variant.DisplayOrder).ThenBy(variant => variant.Name).Take(limit)
            .Select(variant => new ConfigurationAuthoringProductVariantOption
            {
                Id = variant.Id, ProductId = variant.ProductId, ProductCode = variant.Product.Code, ProductName = variant.Product.Name,
                Code = variant.Code, Name = variant.Name, FulfillmentType = variant.FulfillmentType.ToString(),
                IsAvailable = variant.IsAvailable && variant.Product.IsAvailable, OrganizationId = variant.Product.OrganizationId,
                StoreId = variant.Product.StoreId, KioskId = variant.Product.KioskId
            }).ToListAsync(cancellationToken);

        var recipeQuery = _dbContext.Recipes.AsNoTracking().Where(recipe =>
            recipe.DeletedAt == null && recipe.ProductVariant.DeletedAt == null && recipe.ProductVariant.Product.DeletedAt == null &&
            recipe.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced &&
            (recipe.Status == RecipeStatus.Published || recipe.Status == RecipeStatus.Active) &&
            (!recipe.OrganizationId.HasValue || recipe.OrganizationId == organizationId) &&
            (!recipe.ProductVariant.Product.OrganizationId.HasValue || recipe.ProductVariant.Product.OrganizationId == organizationId));
        if (productVariantId.HasValue) recipeQuery = recipeQuery.Where(recipe => recipe.ProductVariantId == productVariantId.Value);
        if (term is not null) recipeQuery = recipeQuery.Where(recipe => EF.Functions.ILike(recipe.Code, $"%{term}%") || EF.Functions.ILike(recipe.Name, $"%{term}%"));
        var recipes = await recipeQuery.OrderBy(recipe => recipe.ProductVariantId).ThenByDescending(recipe => recipe.IsDefault).ThenByDescending(recipe => recipe.Version).Take(limit)
            .Select(recipe => new ConfigurationAuthoringRecipeOption
            {
                Id = recipe.Id, ProductId = recipe.ProductVariant.ProductId, ProductVariantId = recipe.ProductVariantId, ProductVariantCode = recipe.ProductVariant.Code,
                ProductVariantName = recipe.ProductVariant.Name, Code = recipe.Code, Name = recipe.Name, Version = recipe.Version,
                Status = recipe.Status.ToString(), IsDefault = recipe.IsDefault, OrganizationId = recipe.OrganizationId,
                StoreId = recipe.StoreId, KioskId = recipe.KioskId
            }).ToListAsync(cancellationToken);

        var productIds = recipes.Select(recipe => recipe.ProductId).Distinct().ToArray();
        var productionOptions = await _dbContext.ProductOptions.AsNoTracking()
            .Where(option => option.DeletedAt == null &&
                productIds.Contains(option.OptionGroup.ProductId) &&
                option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting)
            .Select(option => new
            {
                ProductId = option.OptionGroup.ProductId,
                option.Id,
                option.Code,
                option.Name,
                OptionGroupCode = option.OptionGroup.Code,
                OptionGroupName = option.OptionGroup.Name,
                GroupIsRequired = option.OptionGroup.IsRequired,
                IsAvailable = option.IsAvailable && option.OptionGroup.IsActive
            })
            .OrderBy(option => option.OptionGroupCode).ThenBy(option => option.Code)
            .ToListAsync(cancellationToken);
        var productionOptionsByProductId = productionOptions
            .GroupBy(option => option.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ConfigurationAuthoringProductionOption>)group.Select(option =>
                    new ConfigurationAuthoringProductionOption
                    {
                        Id = option.Id,
                        Code = option.Code,
                        Name = option.Name,
                        OptionGroupCode = option.OptionGroupCode,
                        OptionGroupName = option.OptionGroupName,
                        GroupIsRequired = option.GroupIsRequired,
                        IsAvailable = option.IsAvailable
                    }).ToArray());
        recipes = recipes.Select(recipe => new ConfigurationAuthoringRecipeOption
        {
            Id = recipe.Id,
            ProductId = recipe.ProductId,
            ProductVariantId = recipe.ProductVariantId,
            ProductVariantCode = recipe.ProductVariantCode,
            ProductVariantName = recipe.ProductVariantName,
            Code = recipe.Code,
            Name = recipe.Name,
            Version = recipe.Version,
            Status = recipe.Status,
            IsDefault = recipe.IsDefault,
            OrganizationId = recipe.OrganizationId,
            StoreId = recipe.StoreId,
            KioskId = recipe.KioskId,
            ProductionOptionCandidates = productionOptionsByProductId.GetValueOrDefault(recipe.ProductId, [])
        }).ToList();

        var programQuery = _dbContext.RobotPrograms.AsNoTracking().Where(program =>
            program.DeletedAt == null && program.Status == RobotProgramStatus.Published &&
            (!program.OrganizationId.HasValue || program.OrganizationId == organizationId));
        if (term is not null) programQuery = programQuery.Where(program => EF.Functions.ILike(program.Code, $"%{term}%") || EF.Functions.ILike(program.Name, $"%{term}%"));
        var programs = await programQuery.OrderBy(program => program.Code).Take(limit)
            .Select(program => new ConfigurationAuthoringRobotProgramOption
            {
                Id = program.Id, Code = program.Code, Name = program.Name, ScopeType = program.ScopeType.ToString(),
                OrganizationId = program.OrganizationId, StoreId = program.StoreId, KioskId = program.KioskId, DeviceId = program.DeviceId,
                ProgramManifestChecksum = program.ProgramManifestChecksum!, ArtifactCount = program.RobotProgramArtifacts.Count
            }).ToListAsync(cancellationToken);

        var programIds = programs.Select(program => program.Id).ToArray();
        var programCapabilityRows = await (
            from membership in _dbContext.RobotProgramArtifacts.AsNoTracking()
            join artifact in _dbContext.RobotArtifacts.AsNoTracking() on membership.RobotArtifactId equals artifact.Id
            join effect in _dbContext.RobotArtifactDeclaredEffects.AsNoTracking() on artifact.TechnicalContractId equals effect.TechnicalContractId
            where programIds.Contains(membership.RobotProgramId) &&
                effect.RequiredWorkcellCapabilityCode != null && effect.RequiredWorkcellCapabilityCode != ""
            select new { membership.RobotProgramId, effect.RequiredWorkcellCapabilityCode })
            .ToListAsync(cancellationToken);
        var capabilitiesByProgramId = programCapabilityRows
            .GroupBy(row => row.RobotProgramId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(row => row.RequiredWorkcellCapabilityCode!.Trim().ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray());
        programs = programs.Select(program => new ConfigurationAuthoringRobotProgramOption
        {
            Id = program.Id,
            Code = program.Code,
            Name = program.Name,
            ScopeType = program.ScopeType,
            OrganizationId = program.OrganizationId,
            StoreId = program.StoreId,
            KioskId = program.KioskId,
            DeviceId = program.DeviceId,
            ProgramManifestChecksum = program.ProgramManifestChecksum,
            ArtifactCount = program.ArtifactCount,
            WorkcellCapabilityCodes = capabilitiesByProgramId.GetValueOrDefault(program.Id, [])
        }).ToList();
        var workcellCapabilities = programCapabilityRows
            .Where(row => !string.IsNullOrWhiteSpace(row.RequiredWorkcellCapabilityCode))
            .GroupBy(row => row.RequiredWorkcellCapabilityCode!.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ConfigurationAuthoringWorkcellCapabilityOption
            {
                Code = group.Key,
                RobotProgramIds = group.Select(row => row.RobotProgramId).Distinct().Order().ToArray()
            })
            .ToArray();
        return new ConfigurationReleaseAuthoringOptionsReadModel
        {
            ProductVariants = variants,
            Recipes = recipes,
            RobotPrograms = programs,
            WorkcellCapabilities = workcellCapabilities
        };
    }

    public async Task<ConfigurationReleaseDiscardOutcome> DiscardDraftReleaseAsync(ConfigurationRelease release, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var routes = release.ExecutionRoutes.ToArray();
        _dbContext.ExecutionRouteRobotBindings.RemoveRange(routes.SelectMany(route => route.RobotBindings));
        _dbContext.ExecutionRoutes.RemoveRange(routes);
        var entry = _dbContext.ConfigurationReleases.Remove(release);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ConfigurationReleaseDiscardOutcome.Deleted;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            entry.State = EntityState.Unchanged;
            return ConfigurationReleaseDiscardOutcome.Referenced;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<ConfigurationRelease> ReleaseGraph(bool asNoTracking = false)
    {
        var query = _dbContext.ConfigurationReleases.WhereNotDeleted();
        if (asNoTracking) query = query.AsNoTracking();
        return query.Include(release => release.ExecutionRoutes).ThenInclude(route => route.ProductVariant).ThenInclude(variant => variant.Product)
                .ThenInclude(product => product.OptionGroups).ThenInclude(group => group.ProductOptions)
                .ThenInclude(option => option.IngredientRequirements).ThenInclude(requirement => requirement.Ingredient)
            .Include(release => release.ExecutionRoutes).ThenInclude(route => route.Recipe)
                .ThenInclude(recipe => recipe.RecipeItems).ThenInclude(item => item.Ingredient)
            .Include(release => release.ExecutionRoutes).ThenInclude(route => route.RobotBindings).ThenInclude(binding => binding.RobotProgram);
    }

    private IQueryable<ConfigurationRelease> BuildReleaseListQuery(Guid? organizationId, ConfigurationReleaseStatus? status, bool isSystemAdmin, IEnumerable<Guid> allowedOrganizationIds)
    {
        var query = _dbContext.ConfigurationReleases.WhereNotDeleted().AsNoTracking();
        if (!isSystemAdmin) query = query.Where(release => allowedOrganizationIds.ToArray().Contains(release.OrganizationId));
        if (organizationId.HasValue) query = query.Where(release => release.OrganizationId == organizationId.Value);
        if (status.HasValue) query = query.Where(release => release.Status == status.Value);
        return query;
    }
}
