using Domain.Catalog.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Programs;

namespace Application.ProductionConfiguration.Routes.Abstractions;

public interface IConfigurationRouteStore
{
    Task<IReadOnlyList<ProductVariant>> ListProductVariantsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Recipe>> ListRecipesByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RobotProgram>> ListRobotProgramsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionProgramBinding>> ListProductionProgramBindingsByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionProgramBinding>> ListActiveProductionProgramBindingsAsync(Guid organizationId,
        IReadOnlyCollection<Guid> recipeIds, IReadOnlyCollection<Guid> robotProgramIds, CancellationToken cancellationToken = default);
    Task SaveReleaseReplacementAsync(IReadOnlyCollection<ExecutionRoute> removedRoutes, CancellationToken cancellationToken = default);
}
