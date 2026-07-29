using Application.Catalog.Ingredients.Requests;

namespace Application.Catalog.Ingredients.Commands;

public sealed record CreateIngredientCommand(CreateIngredientRequest Request, Guid? ActorId);
public sealed record UpdateIngredientCommand(Guid IngredientId, UpdateIngredientRequest Request, Guid? ActorId);
public sealed record SetIngredientStatusCommand(Guid IngredientId, bool IsActive, Guid? ActorId);
public sealed record DeleteIngredientCommand(Guid IngredientId);
