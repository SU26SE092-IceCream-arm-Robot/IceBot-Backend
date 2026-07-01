using Domain.Devices.ExecutionEndpoints;
using Application.ProductionConfiguration.Abstractions;
using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Catalog.Entities;
using Domain.RobotConfiguration.Entities;
using Domain.RobotConfiguration.Enums;
using Application.ProductionConfiguration.ReadModels;
using Domain.Catalog.Enums;
using Npgsql;

namespace Infrastructure.ProductionConfiguration.Persistence;

public sealed class ProductionConfigurationStore : IProductionConfigurationStore
{
    private readonly IceBotDbContext _dbContext;

    public ProductionConfigurationStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteDeploymentCreationAsync<T>(
        Guid executionScopeId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"deployment:{executionScopeId:D}"}, 0));",
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph()
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph()
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<ConfigurationRelease?> GetReleaseByIdAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph(asNoTracking: true)
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<ConfigurationRelease?> GetReleaseForEditAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph()
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentForReconciliationAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<ControllerArtifactSetDeployment?> GetControllerDeploymentForReconciliationAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentByIdempotencyKeyAsync(
        Guid endpointId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments.AsNoTracking()
            .FirstOrDefaultAsync(deployment =>
                deployment.KioskExecutionEndpointId == endpointId &&
                deployment.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public Task<ControllerArtifactSetDeployment?> GetControllerDeploymentByIdempotencyKeyAsync(
        Guid endpointId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments.AsNoTracking()
            .Include(deployment => deployment.Items)
            .FirstOrDefaultAsync(deployment =>
                deployment.KioskExecutionEndpointId == endpointId &&
                deployment.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AnyAsync(
            organization => organization.Id == organizationId && organization.DeletedAt == null,
            cancellationToken);
    }

    public async Task<ConfigurationRelease> CreateNextReleaseAsync(
        Guid organizationId,
        Func<long, ConfigurationRelease> releaseFactory,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({organizationId.ToString("D")}, 0));",
            cancellationToken);

        var maximum = await _dbContext.ConfigurationReleases
            .Where(release => release.OrganizationId == organizationId)
            .Select(release => (long?)release.ReleaseNumber)
            .MaxAsync(cancellationToken);
        var release = releaseFactory((maximum ?? 0) + 1);
        await _dbContext.ConfigurationReleases.AddAsync(release, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return release;
    }

    public Task<int> CountReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        CancellationToken cancellationToken = default)
    {
        return BuildReleaseListQuery(organizationId, status, isSystemAdmin, allowedOrganizationIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationReleaseSummaryReadModel>> ListReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildReleaseListQuery(organizationId, status, isSystemAdmin, allowedOrganizationIds)
            .OrderByDescending(release => release.ReleaseNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(release => new ConfigurationReleaseSummaryReadModel
            {
                Id = release.Id,
                OrganizationId = release.OrganizationId,
                ReleaseNumber = release.ReleaseNumber,
                Status = release.Status.ToString(),
                ReleaseManifestSchemaVersion = release.ReleaseManifestSchemaVersion,
                ReleaseChecksum = release.ReleaseChecksum,
                PublishedAt = release.PublishedAt,
                PublishedByAccountId = release.PublishedByAccountId,
                RouteCount = release.ExecutionRoutes.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductVariant>> ListProductVariantsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariants.AsNoTracking()
            .Include(variant => variant.Product)
            .Where(variant => ids.Contains(variant.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recipe>> ListRecipesByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Recipes.AsNoTracking()
            .Where(recipe => ids.Contains(recipe.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RobotProgram>> ListRobotProgramsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotPrograms.AsNoTracking()
            .Where(program => ids.Contains(program.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConfigurationReleaseAuthoringOptionsReadModel> GetAuthoringOptionsAsync(
        Guid organizationId,
        Guid? productVariantId,
        string? search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var variantQuery = _dbContext.ProductVariants.AsNoTracking()
            .Where(variant => variant.DeletedAt == null && variant.Product.DeletedAt == null &&
                variant.FulfillmentType == FulfillmentType.MachineProduced &&
                (!variant.Product.OrganizationId.HasValue || variant.Product.OrganizationId == organizationId));
        if (productVariantId.HasValue)
            variantQuery = variantQuery.Where(variant => variant.Id == productVariantId.Value);
        if (term is not null)
            variantQuery = variantQuery.Where(variant =>
                EF.Functions.ILike(variant.Code, $"%{term}%") ||
                EF.Functions.ILike(variant.Name, $"%{term}%") ||
                EF.Functions.ILike(variant.Product.Code, $"%{term}%") ||
                EF.Functions.ILike(variant.Product.Name, $"%{term}%"));

        var variants = await variantQuery
            .OrderBy(variant => variant.Product.Name)
            .ThenBy(variant => variant.DisplayOrder)
            .ThenBy(variant => variant.Name)
            .Take(limit)
            .Select(variant => new ConfigurationAuthoringProductVariantOption
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                ProductCode = variant.Product.Code,
                ProductName = variant.Product.Name,
                Code = variant.Code,
                Name = variant.Name,
                FulfillmentType = variant.FulfillmentType.ToString(),
                IsAvailable = variant.IsAvailable && variant.Product.IsAvailable,
                OrganizationId = variant.Product.OrganizationId,
                StoreId = variant.Product.StoreId,
                KioskId = variant.Product.KioskId
            })
            .ToListAsync(cancellationToken);

        var recipeQuery = _dbContext.Recipes.AsNoTracking()
            .Where(recipe => recipe.DeletedAt == null &&
                recipe.ProductVariant.DeletedAt == null &&
                recipe.ProductVariant.Product.DeletedAt == null &&
                recipe.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced &&
                (recipe.Status == Domain.Catalog.Enums.RecipeStatus.Published ||
                    recipe.Status == Domain.Catalog.Enums.RecipeStatus.Active) &&
                (!recipe.OrganizationId.HasValue || recipe.OrganizationId == organizationId) &&
                (!recipe.ProductVariant.Product.OrganizationId.HasValue ||
                    recipe.ProductVariant.Product.OrganizationId == organizationId));
        if (productVariantId.HasValue)
            recipeQuery = recipeQuery.Where(recipe => recipe.ProductVariantId == productVariantId.Value);
        if (term is not null)
            recipeQuery = recipeQuery.Where(recipe =>
                EF.Functions.ILike(recipe.Code, $"%{term}%") ||
                EF.Functions.ILike(recipe.Name, $"%{term}%"));

        var recipes = await recipeQuery
            .OrderBy(recipe => recipe.ProductVariantId)
            .ThenByDescending(recipe => recipe.IsDefault)
            .ThenByDescending(recipe => recipe.Version)
            .Take(limit)
            .Select(recipe => new ConfigurationAuthoringRecipeOption
            {
                Id = recipe.Id,
                ProductVariantId = recipe.ProductVariantId,
                ProductVariantCode = recipe.ProductVariant.Code,
                ProductVariantName = recipe.ProductVariant.Name,
                Code = recipe.Code,
                Name = recipe.Name,
                Version = recipe.Version,
                Status = recipe.Status.ToString(),
                IsDefault = recipe.IsDefault,
                OrganizationId = recipe.OrganizationId,
                StoreId = recipe.StoreId,
                KioskId = recipe.KioskId
            })
            .ToListAsync(cancellationToken);

        var programQuery = _dbContext.RobotPrograms.AsNoTracking()
            .Where(program => program.DeletedAt == null &&
                program.Status == RobotProgramStatus.Published &&
                (!program.OrganizationId.HasValue || program.OrganizationId == organizationId));
        if (term is not null)
            programQuery = programQuery.Where(program =>
                EF.Functions.ILike(program.Code, $"%{term}%") ||
                EF.Functions.ILike(program.Name, $"%{term}%"));

        var programs = await programQuery
            .OrderBy(program => program.Code)
            .Take(limit)
            .Select(program => new ConfigurationAuthoringRobotProgramOption
            {
                Id = program.Id,
                Code = program.Code,
                Name = program.Name,
                ScopeType = program.ScopeType.ToString(),
                OrganizationId = program.OrganizationId,
                StoreId = program.StoreId,
                KioskId = program.KioskId,
                DeviceId = program.DeviceId,
                ProgramManifestChecksum = program.ProgramManifestChecksum!,
                ArtifactCount = program.RobotProgramArtifacts.Count
            })
            .ToListAsync(cancellationToken);

        return new ConfigurationReleaseAuthoringOptionsReadModel
        {
            ProductVariants = variants,
            Recipes = recipes,
            RobotPrograms = programs
        };
    }

    public Task<int> CountConfigurationDeploymentsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? configurationReleaseId,
        ConfigurationDeploymentProfile? profile,
        ConfigurationDeploymentReadStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        return BuildConfigurationDeploymentQuery(
            organizationId, storeId, kioskId, configurationReleaseId, profile, status,
            isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationDeploymentReadModel>> ListConfigurationDeploymentsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? configurationReleaseId,
        ConfigurationDeploymentProfile? profile,
        ConfigurationDeploymentReadStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildConfigurationDeploymentQuery(
                organizationId, storeId, kioskId, configurationReleaseId, profile, status,
                isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .OrderByDescending(deployment => deployment.RequestedAt)
            .ThenByDescending(deployment => deployment.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<ConfigurationDeploymentReadModel?> GetConfigurationDeploymentAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return FullEdgeDeploymentQuery().Where(deployment => deployment.Id == deploymentId)
            .Concat(LowCostDeploymentQuery().Where(deployment => deployment.Id == deploymentId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentForRollbackAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments.AsNoTracking()
            .Include(deployment => deployment.Items)
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<KioskExecutionEndpoint?> GetEndpointForDeploymentAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.SupportedRobotTargets)
                .ThenInclude(target => target.Device)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public Task<bool> HasPendingFullEdgeDeploymentAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments.AnyAsync(
            deployment => deployment.KioskId == kioskId &&
                (deployment.Status == KioskConfigurationDeploymentStatus.Pending ||
                    deployment.Status == KioskConfigurationDeploymentStatus.Installed),
            cancellationToken);
    }

    public async Task<int> GetNextFullEdgeDeploymentAttemptNoAsync(
        Guid kioskId,
        Guid configurationReleaseId,
        CancellationToken cancellationToken = default)
    {
        var maxAttempt = await _dbContext.KioskConfigurationDeployments
            .Where(deployment => deployment.KioskId == kioskId && deployment.ConfigurationReleaseId == configurationReleaseId)
            .Select(deployment => (int?)deployment.AttemptNo)
            .MaxAsync(cancellationToken);

        return (maxAttempt ?? 0) + 1;
    }

    public Task<bool> HasPendingControllerArtifactSetDeploymentAsync(Guid controllerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments.AnyAsync(
            deployment => deployment.ControllerId == controllerId &&
                (deployment.Status == ControllerArtifactSetDeploymentStatus.Pending ||
                    deployment.Status == ControllerArtifactSetDeploymentStatus.Installed),
            cancellationToken);
    }

    public async Task<bool> ReleaseHasPendingDeploymentAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default)
    {
        var hasFullEdge = await _dbContext.KioskConfigurationDeployments.AnyAsync(
            deployment => deployment.ConfigurationReleaseId == releaseId &&
                (deployment.Status == KioskConfigurationDeploymentStatus.Pending ||
                    deployment.Status == KioskConfigurationDeploymentStatus.Installed),
            cancellationToken);
        if (hasFullEdge)
            return true;

        return await _dbContext.ControllerArtifactSetDeployments.AnyAsync(
            deployment => deployment.SourceConfigurationReleaseId == releaseId &&
                (deployment.Status == ControllerArtifactSetDeploymentStatus.Pending ||
                    deployment.Status == ControllerArtifactSetDeploymentStatus.Installed),
            cancellationToken);
    }

    public Task<int> FailFullEdgeDeploymentsMissingAcceptedCommandReportAsync(
        DateTimeOffset acceptedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default)
    {
        var timedOutIds = _dbContext.KioskConfigurationDeployments
            .Where(deployment => deployment.Status == KioskConfigurationDeploymentStatus.Pending &&
                _dbContext.EdgeCommands.Any(command =>
                    command.CommandType == Domain.Sync.Enums.EdgeCommandType.DeployConfiguration &&
                    command.DeploymentKind == Domain.Sync.Enums.DeploymentCommandTargetKind.FullEdgeConfiguration &&
                    command.DeploymentId == deployment.Id &&
                    command.Status == Domain.Sync.Enums.EdgeCommandStatus.Accepted &&
                    command.RespondedAt != null &&
                    command.RespondedAt < acceptedBefore))
            .OrderBy(deployment => deployment.RequestedAt)
            .Take(maxDeployments)
            .Select(deployment => deployment.Id);

        return _dbContext.KioskConfigurationDeployments
            .Where(deployment => timedOutIds.Contains(deployment.Id) &&
                deployment.Status == KioskConfigurationDeploymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(deployment => deployment.Status, KioskConfigurationDeploymentStatus.Failed)
                .SetProperty(deployment => deployment.FailureCode, "ExecutionReportTimeout")
                .SetProperty(deployment => deployment.FailureReason,
                    "The execution endpoint accepted the deployment command but did not report an installation result before the timeout.")
                .SetProperty(deployment => deployment.UpdatedAt, observedAt),
                cancellationToken);
    }

    public Task<int> FailControllerDeploymentsMissingAcceptedCommandReportAsync(
        DateTimeOffset acceptedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default)
    {
        var timedOutIds = _dbContext.ControllerArtifactSetDeployments
            .Where(deployment => deployment.Status == ControllerArtifactSetDeploymentStatus.Pending &&
                _dbContext.EdgeCommands.Any(command =>
                    command.CommandType == Domain.Sync.Enums.EdgeCommandType.DeployConfiguration &&
                    command.DeploymentKind == Domain.Sync.Enums.DeploymentCommandTargetKind.LowCostArtifactSet &&
                    command.DeploymentId == deployment.Id &&
                    command.Status == Domain.Sync.Enums.EdgeCommandStatus.Accepted &&
                    command.RespondedAt != null &&
                    command.RespondedAt < acceptedBefore))
            .OrderBy(deployment => deployment.RequestedAt)
            .Take(maxDeployments)
            .Select(deployment => deployment.Id);

        return _dbContext.ControllerArtifactSetDeployments
            .Where(deployment => timedOutIds.Contains(deployment.Id) &&
                deployment.Status == ControllerArtifactSetDeploymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(deployment => deployment.Status, ControllerArtifactSetDeploymentStatus.Failed)
                .SetProperty(deployment => deployment.FailureCode, "ExecutionReportTimeout")
                .SetProperty(deployment => deployment.FailureReason,
                    "The execution endpoint accepted the deployment command but did not report an installation result before the timeout.")
                .SetProperty(deployment => deployment.UpdatedAt, observedAt),
                cancellationToken);
    }

    public Task<int> FailFullEdgeDeploymentsMissingActivationReportAsync(
        DateTimeOffset installedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default)
    {
        var timedOutIds = _dbContext.KioskConfigurationDeployments
            .Where(deployment => deployment.Status == KioskConfigurationDeploymentStatus.Installed &&
                deployment.CloudReceivedAt != null && deployment.CloudReceivedAt < installedBefore)
            .OrderBy(deployment => deployment.CloudReceivedAt)
            .Take(maxDeployments)
            .Select(deployment => deployment.Id);
        return _dbContext.KioskConfigurationDeployments
            .Where(deployment => timedOutIds.Contains(deployment.Id) && deployment.Status == KioskConfigurationDeploymentStatus.Installed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(deployment => deployment.Status, KioskConfigurationDeploymentStatus.Failed)
                .SetProperty(deployment => deployment.FailureCode, "ActivationReportTimeout")
                .SetProperty(deployment => deployment.FailureReason, "The execution endpoint installed the deployment but did not report activation before the timeout.")
                .SetProperty(deployment => deployment.UpdatedAt, observedAt), cancellationToken);
    }

    public Task<int> FailControllerDeploymentsMissingActivationReportAsync(
        DateTimeOffset installedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default)
    {
        var timedOutIds = _dbContext.ControllerArtifactSetDeployments
            .Where(deployment => deployment.Status == ControllerArtifactSetDeploymentStatus.Installed &&
                deployment.CloudReceivedAt != null && deployment.CloudReceivedAt < installedBefore)
            .OrderBy(deployment => deployment.CloudReceivedAt)
            .Take(maxDeployments)
            .Select(deployment => deployment.Id);
        return _dbContext.ControllerArtifactSetDeployments
            .Where(deployment => timedOutIds.Contains(deployment.Id) && deployment.Status == ControllerArtifactSetDeploymentStatus.Installed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(deployment => deployment.Status, ControllerArtifactSetDeploymentStatus.Failed)
                .SetProperty(deployment => deployment.FailureCode, "ActivationReportTimeout")
                .SetProperty(deployment => deployment.FailureReason, "The controller installed the artifact set but did not report activation before the timeout.")
                .SetProperty(deployment => deployment.UpdatedAt, observedAt), cancellationToken);
    }

    public async Task<long> GetNextControllerActiveSetVersionAsync(Guid controllerId, CancellationToken cancellationToken = default)
    {
        var maxVersion = await _dbContext.ControllerArtifactSetDeployments
            .Where(deployment => deployment.ControllerId == controllerId)
            .Select(deployment => (long?)deployment.ActiveSetVersion)
            .MaxAsync(cancellationToken);

        return (maxVersion ?? 0) + 1;
    }

    public Task AddFullEdgeDeploymentAsync(KioskConfigurationDeployment deployment, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments.AddAsync(deployment, cancellationToken).AsTask();
    }

    public Task AddControllerArtifactSetDeploymentAsync(ControllerArtifactSetDeployment deployment, CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments.AddAsync(deployment, cancellationToken).AsTask();
    }

    public async Task SaveReleaseReplacementAsync(
        IReadOnlyCollection<ExecutionRoute> routes,
        CancellationToken cancellationToken = default)
    {
        var routeArray = routes.ToArray();
        _dbContext.ExecutionRouteRobotBindings.RemoveRange(routeArray.SelectMany(route => route.RobotBindings));
        _dbContext.ExecutionRoutes.RemoveRange(routeArray);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConfigurationReleaseDiscardOutcome> DiscardDraftReleaseAsync(
        ConfigurationRelease release,
        CancellationToken cancellationToken = default)
    {
        var hasDeployment = await _dbContext.KioskConfigurationDeployments.AnyAsync(
                deployment => deployment.ConfigurationReleaseId == release.Id,
                cancellationToken) ||
            await _dbContext.ControllerArtifactSetDeployments.AnyAsync(
                deployment => deployment.SourceConfigurationReleaseId == release.Id,
                cancellationToken);
        if (hasDeployment)
        {
            return ConfigurationReleaseDiscardOutcome.Referenced;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var routeArray = release.ExecutionRoutes.ToArray();
        _dbContext.ExecutionRouteRobotBindings.RemoveRange(routeArray.SelectMany(route => route.RobotBindings));
        _dbContext.ExecutionRoutes.RemoveRange(routeArray);
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ConfigurationRelease> ReleaseGraph(bool asNoTracking = false)
    {
        var query = _dbContext.ConfigurationReleases.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.ProductVariant)
                    .ThenInclude(productVariant => productVariant.Product)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.Recipe)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.RobotBindings)
                    .ThenInclude(binding => binding.RobotProgram)
                        .ThenInclude(program => program.RobotProgramArtifacts)
                            .ThenInclude(programArtifact => programArtifact.RobotArtifact);
    }

    private IQueryable<ConfigurationRelease> BuildReleaseListQuery(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds)
    {
        var query = _dbContext.ConfigurationReleases.AsNoTracking();
        if (!isSystemAdmin)
        {
            var organizationIds = allowedOrganizationIds.ToArray();
            query = query.Where(release => organizationIds.Contains(release.OrganizationId));
        }

        if (organizationId.HasValue)
        {
            query = query.Where(release => release.OrganizationId == organizationId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(release => release.Status == status.Value);
        }

        return query;
    }

    private IQueryable<ConfigurationDeploymentReadModel> BuildConfigurationDeploymentQuery(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? configurationReleaseId,
        ConfigurationDeploymentProfile? profile,
        ConfigurationDeploymentReadStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds)
    {
        IQueryable<ConfigurationDeploymentReadModel> query = profile switch
        {
            ConfigurationDeploymentProfile.FullEdge => FullEdgeDeploymentQuery(),
            ConfigurationDeploymentProfile.LowCostController => LowCostDeploymentQuery(),
            _ => FullEdgeDeploymentQuery().Concat(LowCostDeploymentQuery())
        };

        if (!isSystemAdmin)
        {
            var organizationIds = allowedOrganizationIds.ToArray();
            var storeIds = allowedStoreIds.ToArray();
            var kioskIds = allowedKioskIds.ToArray();
            query = query.Where(deployment =>
                organizationIds.Contains(deployment.OrganizationId) ||
                storeIds.Contains(deployment.StoreId) ||
                kioskIds.Contains(deployment.KioskId));
        }

        if (organizationId.HasValue) query = query.Where(deployment => deployment.OrganizationId == organizationId.Value);
        if (storeId.HasValue) query = query.Where(deployment => deployment.StoreId == storeId.Value);
        if (kioskId.HasValue) query = query.Where(deployment => deployment.KioskId == kioskId.Value);
        if (configurationReleaseId.HasValue) query = query.Where(deployment => deployment.ConfigurationReleaseId == configurationReleaseId.Value);
        if (status.HasValue) query = query.Where(deployment => deployment.Status == status.Value);
        return query;
    }

    private IQueryable<ConfigurationDeploymentReadModel> FullEdgeDeploymentQuery()
    {
        return _dbContext.KioskConfigurationDeployments.AsNoTracking()
            .Select(deployment => new ConfigurationDeploymentReadModel
            {
                Id = deployment.Id,
                Profile = ConfigurationDeploymentProfile.FullEdge,
                OrganizationId = deployment.KioskExecutionEndpoint.Kiosk.OrganizationId,
                StoreId = deployment.KioskExecutionEndpoint.Kiosk.StoreId,
                KioskId = deployment.KioskId,
                KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
                EndpointCode = deployment.KioskExecutionEndpoint.EndpointCode,
                ConfigurationReleaseId = deployment.ConfigurationReleaseId,
                ReleaseNumber = deployment.ConfigurationRelease.ReleaseNumber,
                ReleaseChecksum = deployment.ReleaseChecksum,
                Status = (ConfigurationDeploymentReadStatus)deployment.Status,
                RequestedAt = deployment.RequestedAt,
                RequestedByAccountId = deployment.RequestedByAccountId,
                ExecutorReportedAt = deployment.EdgeReportedAt,
                CloudReceivedAt = deployment.CloudReceivedAt,
                LastReportId = deployment.LastEdgeDeploymentEventId,
                FailureCode = deployment.FailureCode,
                FailureReason = deployment.FailureReason,
                AttemptNo = deployment.AttemptNo,
                EdgeRuntimeId = deployment.EdgeRuntimeId,
                ControllerId = null,
                ActiveSetVersion = null,
                ActiveSetChecksum = null,
                RequestedArtifactCount = null,
                RequestedArtifactStorageBytes = null,
                MaxArtifactCount = null,
                MaxArtifactStorageBytes = null
            });
    }

    private IQueryable<ConfigurationDeploymentReadModel> LowCostDeploymentQuery()
    {
        return _dbContext.ControllerArtifactSetDeployments.AsNoTracking()
            .Select(deployment => new ConfigurationDeploymentReadModel
            {
                Id = deployment.Id,
                Profile = ConfigurationDeploymentProfile.LowCostController,
                OrganizationId = deployment.KioskExecutionEndpoint.Kiosk.OrganizationId,
                StoreId = deployment.KioskExecutionEndpoint.Kiosk.StoreId,
                KioskId = deployment.KioskId,
                KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
                EndpointCode = deployment.KioskExecutionEndpoint.EndpointCode,
                ConfigurationReleaseId = deployment.SourceConfigurationReleaseId,
                ReleaseNumber = deployment.SourceConfigurationRelease.ReleaseNumber,
                ReleaseChecksum = deployment.ReleaseChecksum,
                Status = (ConfigurationDeploymentReadStatus)deployment.Status,
                RequestedAt = deployment.RequestedAt,
                RequestedByAccountId = deployment.RequestedByAccountId,
                ExecutorReportedAt = deployment.ControllerReportedAt,
                CloudReceivedAt = deployment.CloudReceivedAt,
                LastReportId = deployment.LastControllerReportId,
                FailureCode = deployment.FailureCode,
                FailureReason = deployment.FailureReason,
                AttemptNo = null,
                EdgeRuntimeId = null,
                ControllerId = deployment.ControllerId,
                ActiveSetVersion = deployment.ActiveSetVersion,
                ActiveSetChecksum = deployment.ActiveSetChecksum,
                RequestedArtifactCount = deployment.RequestedArtifactCount,
                RequestedArtifactStorageBytes = deployment.RequestedArtifactStorageBytes,
                MaxArtifactCount = deployment.MaxArtifactCount,
                MaxArtifactStorageBytes = deployment.MaxArtifactStorageBytes
            });
    }
}
