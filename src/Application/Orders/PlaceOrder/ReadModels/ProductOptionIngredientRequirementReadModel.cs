namespace Application.Orders.PlaceOrder.ReadModels;

public sealed record ProductOptionIngredientRequirementReadModel(
    Guid ProductOptionId,
    Guid IngredientId,
    string IngredientCode,
    string IngredientName,
    decimal Quantity,
    string Unit,
    string RequiredWorkcellCapabilityCode,
    bool IsIngredientActive);
