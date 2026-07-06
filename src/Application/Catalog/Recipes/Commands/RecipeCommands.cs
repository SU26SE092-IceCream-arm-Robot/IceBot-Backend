using Application.Catalog.Products.Commands;
using Application.Catalog.Recipes.Requests;

namespace Application.Catalog.Recipes.Commands;

public sealed record CreateRecipeCommand(
    ProductManagementCommandScope Scope,
    Guid ProductId,
    Guid VariantId,
    CreateRecipeRequest Request,
    Guid? ActorId);

public sealed record UpdateRecipeCommand(
    ProductManagementCommandScope Scope,
    Guid ProductId,
    Guid VariantId,
    Guid RecipeId,
    UpdateRecipeRequest Request,
    Guid? ActorId);

public sealed record ReplaceRecipeItemsCommand(
    ProductManagementCommandScope Scope,
    Guid ProductId,
    Guid VariantId,
    Guid RecipeId,
    ReplaceRecipeItemsRequest Request,
    Guid? ActorId);

public sealed record SetRecipeStatusCommand(
    ProductManagementCommandScope Scope,
    Guid ProductId,
    Guid VariantId,
    Guid RecipeId,
    Domain.Catalog.Enums.RecipeStatus Status,
    Guid? ActorId);

public sealed record CreateRecipeVersionCommand(
    ProductManagementCommandScope Scope,
    Guid ProductId,
    Guid VariantId,
    Guid SourceRecipeId,
    Guid? ActorId);
