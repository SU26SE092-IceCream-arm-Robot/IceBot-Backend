using Application.Inventory.Results;

namespace Application.Inventory.Abstractions;

public interface IInventoryReadinessEvaluator
{
    Task<KioskInventoryReadinessResult?> EvaluateKioskAsync(
        Guid kioskId,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KioskInventoryReadinessResult>> EvaluateOrganizationAsync(
        Guid organizationId,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken = default);
}

public sealed record InventoryReadinessRouteInput(
    Guid ExecutionRouteId,
    string RouteCode,
    Guid RecipeId,
    Guid? ProductOrganizationId,
    Guid? ProductStoreId,
    Guid? ProductKioskId,
    Guid? RecipeOrganizationId,
    Guid? RecipeStoreId,
    Guid? RecipeKioskId);
