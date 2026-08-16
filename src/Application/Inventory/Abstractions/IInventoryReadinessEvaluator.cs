using Application.Inventory.Results;

namespace Application.Inventory.Abstractions;

public interface IInventoryReadinessEvaluator
{
    Task<KioskInventoryReadinessResult?> EvaluateKioskAsync(
        Guid kioskId,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken = default,
        InventoryReadinessEvaluationOptions? options = null);

    Task<IReadOnlyList<KioskInventoryReadinessResult>> EvaluateOrganizationAsync(
        Guid organizationId,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken = default,
        InventoryReadinessEvaluationOptions? options = null);
}

public enum InventoryReadinessEvaluationPurpose
{
    TopologyValidation = 0,
    RuntimeSellability = 1
}

public sealed record InventoryReadinessEvaluationOptions(
    InventoryReadinessEvaluationPurpose Purpose = InventoryReadinessEvaluationPurpose.TopologyValidation,
    DateTimeOffset? ObservedAt = null,
    TimeSpan? MaximumSensorEvidenceAge = null);

public sealed record InventoryIngredientRequirementInput(
    Guid IngredientId,
    string IngredientCode,
    string IngredientName,
    decimal Quantity,
    string Unit);

public sealed record InventoryReadinessRouteInput(
    Guid ExecutionRouteId,
    string RouteCode,
    Guid ProductId,
    Guid RecipeId,
    IReadOnlySet<string> SupportedOptionCodes,
    Guid? ProductOrganizationId,
    Guid? ProductStoreId,
    Guid? ProductKioskId,
    Guid? RecipeOrganizationId,
    Guid? RecipeStoreId,
    Guid? RecipeKioskId,
    int RequestedQuantity = 1,
    IReadOnlyCollection<InventoryIngredientRequirementInput>? SelectedOptionIngredients = null);
